using System.ComponentModel;

namespace ProjectBullet.Avalonia.Helpers
{
    // In Avalonia, sorting is handled differently (via DataGrid or custom logic).
    // This class retains the sort direction info for compatibility.
    public class SortAdorner
    {
        public ListSortDirection Direction { get; private set; }

        public SortAdorner(ListSortDirection dir)
        {
            Direction = dir;
        }
    }
}
