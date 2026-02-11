using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProjectBullet.Avalonia.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private static readonly ConcurrentDictionary<System.Type, PropertyInfo[]> PropertyCache = new();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public virtual void UpdateViewModel()
        {
            var properties = PropertyCache.GetOrAdd(GetType(), t => t.GetProperties());
            foreach (var property in properties)
            {
                OnPropertyChanged(property.Name);
            }
        }
    }
}
