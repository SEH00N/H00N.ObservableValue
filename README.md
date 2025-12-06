# ObservableValue
C# Value Observing

## Usage
```cs
public partial class MyClass
{
    [Observable]
    private float _myField = 0;
}

// somewhere
MyClass myClass = null;
myClass.OnMyFieldChangedEvent += HandleMyFieldChanged;
myClass.MyField = 10;
myClass.MyField = 11;
myClass.MyField = 9;

void HandleMyFieldChanged(float oldValue, float newValue)
{
    Console.WriteLine($"Value Changed! OldValue: {oldValue}, NewValue: {newValue}");
}

// output
Value Changed! OldValue: 0, NewValue: 10
Value Changed! OldValue: 10, NewValue: 11
Value Changed! OldValue: 11, NewValue: 9
```