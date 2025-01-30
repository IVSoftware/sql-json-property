using System.Collections.ObjectModel;
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

ObservableCollection<MyClass> MyClassData = new ObservableCollection<MyClass>();

public class MySubClass
{
    public string mystring3 { get; set; }
    public override string ToString() => mystring3;
}

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