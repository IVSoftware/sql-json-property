If I'm following your intention here, you want to have a column in your table that contains a collection type not supported by the ORM (and this means "whatever" that ORM happens to be).

Here's solution that works really well for me, and it really doesn't matter _what_ type of data that field might hold. I want to show a "real" database interactio and you don't specify an ORM in the tags so I hope it's OK if I use sqlite-net-pcl to do that mock. Granted, that ORM doesn't have a `SqlDataReader` but I don't think the question is essentially about `SQLiteDataReader`.

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

This will write three records, then query one of them and output its string represntation to the console window.

~~~
using SQLite;
using Newtonsoft.Json;

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

    var loopback = someORMConnection
        .Table<MyClass>()
        .FirstOrDefault(_ => _.mystring1 == "Hotel");

    Console.WriteLine(loopback);
    Console.ReadKey();
}
~~~


___

DB Browser for SQLite 


