
// namespace ObservableValue.Tests
// {
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
// }

public partial class Program
{
    public static void Main()
    {
        // ObservableValue.Tests.MyClass myClass = new ObservableValue.Tests.MyClass();
        // myClass.MyField = 10;
    }
}