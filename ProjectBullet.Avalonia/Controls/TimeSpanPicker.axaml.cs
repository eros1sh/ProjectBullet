using System;
using Avalonia;
using Avalonia.Controls;

namespace ProjectBullet.Avalonia.Controls
{
    /// <summary>
    /// Interaction logic for TimeSpanPicker.axaml
    /// </summary>
    public partial class TimeSpanPicker : UserControl
    {
        public TimeSpan TimeSpan
        {
            get => GetValue(TimeSpanProperty);
            set => SetValue(TimeSpanProperty, value);
        }

        public static readonly StyledProperty<TimeSpan> TimeSpanProperty =
            AvaloniaProperty.Register<TimeSpanPicker, TimeSpan>(nameof(TimeSpan));

        static TimeSpanPicker()
        {
            TimeSpanProperty.Changed.AddClassHandler<TimeSpanPicker>((picker, e) =>
            {
                var newValue = (TimeSpan)e.NewValue;
                picker.hours.Value = newValue.Hours;
                picker.minutes.Value = newValue.Minutes;
                picker.seconds.Value = newValue.Seconds;
            });
        }

        public TimeSpanPicker()
        {
            InitializeComponent();
        }

        private void NumberChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            if (hours is not null && minutes is not null && seconds is not null)
            {
                TimeSpan = new TimeSpan((int)(hours.Value ?? 0), (int)(minutes.Value ?? 0), (int)(seconds.Value ?? 0));
            }
        }
    }
}
