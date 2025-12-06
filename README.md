# ObservableValue
C# Value Observing

## Install
```
dotnet add package H00N.ObservableValue
```

## Usage
```cs
public partial class MyClass
{
    [ObservableValue]
    private float _myField = 0;
}

// somewhere
MyClass myClass = new MyClass();
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