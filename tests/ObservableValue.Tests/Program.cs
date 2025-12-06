namespace ObservableValue.Tests
{
    using H00N.ObservableValue;

    public partial class MyClass
    {
        [ObservableValue]
        private float _myField = 0;

        [ObservableValue]
        private float _MYField2 = 0;

        [ObservableValue]
        private Dictionary<string, int> _myField3 = new Dictionary<string, int>();
    }
}

public partial class Program
{
    public static void Main()
    {
        ObservableValue.Tests.MyClass myClass = new ObservableValue.Tests.MyClass();
        myClass.OnMyFieldChangedEvent += HandleMyFieldChanged;
        myClass.MyField = 10;
        myClass.MyField = 11;
        myClass.MyField = 9;
    }

    private static void HandleMyFieldChanged(float oldValue, float newValue)
    {
        Console.WriteLine($"MyField changed from {oldValue} to {newValue}");
    }
}