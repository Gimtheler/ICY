using ICY.Infrastructure;

namespace ICY.Models;

public sealed class SavedOverlayPosition : ObservableObject
{
    private string _name = "Position";
    private double _x;
    private double _y;

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                OnPropertyChanged(nameof(Label));
            }
        }
    }

    public double X
    {
        get => _x;
        set
        {
            if (SetField(ref _x, value))
            {
                OnPropertyChanged(nameof(Label));
            }
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (SetField(ref _y, value))
            {
                OnPropertyChanged(nameof(Label));
            }
        }
    }

    public string Label => $"{Name}  ({X:0},{Y:0})";

    public override string ToString() => Label;
}
