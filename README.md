Let me see whether I'm following your intent here. It seems that you want to have a column in your table that contains "some `<T>`" (which in your case is `List<MySubclass>`) not supported by the ORM ("whatever" that ORM happens to be). I'd like to share a solution that works really well for me, and it really doesn't matter _what_ type of data that field might hold. 
___
*Since I want to show a "real" database interaction and you haven't (yet) specified an ORM in the tags, I hope it's OK if I use [sqlite-net-pcl](https://www.nuget.org/packages/sqlite-net-pcl) to do this mock. Granted, your post shows `SqlDataReader` and that ORM doesn't have one, but I don't think the question is essentially about `SQLiteDataReader`.*
___

**Proposed Solution:** 

Store the `<T>` by serializing to JSON, then make this transparent to the class user by doing a little compensatory name mapping.
___

- _First_ you take the `MySubClassData` property that you show in `MyClass` and hide it from the data reader using whatever is appropriate in the ORM idiom.
- _Next_ you make a string property that will be a JSON serialization of that arbitrary class (and optionally map the name of it to e.g. `MySubclassData`);

This leaves you with classes that might look similar to this.

~~~
public class MySubClass
{
    public string mystring3 { get; set; }
    public override string ToString() => mystring3;
}
~~~

~~~
public class MyClass
{
    public string mystring1 { get; set; } = String.Empty;
    public string mystring2 { get; set; } = String.Empty;

    [Ignore]
    public List<MySubClass> MySubClassData  { get; set; }

    [JsonIgnore]
    [Column("MySubClassData")]
    public string MySubClassDataJSON
    { 
        get => JsonConvert.SerializeObject(MySubClassData, Formatting.Indented);
        set => 
            MySubClassData = 
            JsonConvert.DeserializeObject<List<MySubClass>>(value ?? "[]");
    }
    public override string ToString()
    {
        return $@"
{mystring1}
{mystring2}
{"\t"}{string.Join($"{Environment.NewLine}\t", MySubClassData ?? new List<MySubClass>())}
".Trim();
    }
}
~~~

___

**Minimal Example using a Console App**

This will write three records, query one of them and output its string representation to the console, then perform a query to populate the `ObservableCollection<MyClass>` relying on the `CollectionChenged` handler to output the values to the console as they are added..

[![console output][1]][1]

~~~
using SQLite;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

ObservableCollection<MyClass> Items = new();
Items.CollectionChanged += (sender, e) =>
{
    switch (e.Action)
    {
        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
            if(e.NewItems != null)
            {
                foreach(var record in e.NewItems.OfType<MyClass>())
                {
                    Console.WriteLine(record);
                }
            }
            break;
    }
};
Console.Title = "Json SQL Property Demo";
string _pathToDB =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StackOverflow",
        "DbColumnUsingJson",
        "demo.db");
Directory.CreateDirectory(Path.GetDirectoryName(_pathToDB));

// The ORM is not tagged in the question.
// We will use sqlite-net-pcl to demo the concept.
using(var someORMConnection = new SQLiteConnection(_pathToDB))
{
    someORMConnection.DropTable<MyClass>();
    someORMConnection.CreateTable<MyClass>();
    MyClass[] testData =
    [
        new MyClass
        {
            mystring1 = "Alpha",
            mystring2 = "Bravo",
            MySubClassData = new List<MySubClass>
            {
                new MySubClass { mystring3 = "Charlie" }
            }
        },
        new MyClass
        {
            mystring1 = "Delta",
            mystring2 = "Echo",
            MySubClassData = new List<MySubClass>
            {
                new MySubClass { mystring3 = "Foxtrot" },
                new MySubClass { mystring3 = "Golf" }
            }
        }, 
        new MyClass
        {
            mystring1 = "Hotel",
            mystring2 = "India",
            MySubClassData = new List<MySubClass>
            {
                new MySubClass { mystring3 = "Juliet" },
                new MySubClass { mystring3 = "Kilo" },
                new MySubClass { mystring3 = "Lima" }
            }
        }
    ];
    someORMConnection.InsertAll(testData);

    Console.WriteLine("TEST: Query a single record and display it in the console.");
    Console.WriteLine("===================================================");
    var loopback = someORMConnection
        .Table<MyClass>()
        .FirstOrDefault(_ => _.mystring1 == "Hotel");

    Console.WriteLine(loopback);
    Console.WriteLine();


    Console.WriteLine("TEST: ObservableCollection from Query.");
    Console.WriteLine("=====================================");
    Items.Clear();
    someORMConnection
        .Table<MyClass>()
        .ToList()
        .ForEach(_=> Items.Add(_));

    Console.ReadKey();
}
~~~


___

**DB Browser for SQLite** 

[![database contents][2]][2]


  [1]: https://i.sstatic.net/JpneyzU2.png
  [2]: https://i.sstatic.net/TM0IfjyJ.png