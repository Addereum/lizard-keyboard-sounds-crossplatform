using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace LizardCrossPlatform
{
    public partial class SettingsWindow : Window
    {
        public event EventHandler<float>? VolumeChanged;

        public SettingsWindow()
        {
            InitializeComponent();
            
            var slider = this.FindControl<Slider>("VolumeSlider");
            var label = this.FindControl<TextBlock>("VolumeLabel");
            var button = this.FindControl<Button>("CloseButton");

            if (slider != null && label != null)
            {
                slider.PropertyChanged += (s, e) => {
                    if (e.Property.Name == "Value")
                    {
                        float val = (float)(slider.Value / 100f);
                        label.Text = $"Volume: {(int)slider.Value}%";
                        VolumeChanged?.Invoke(this, val);
                    }
                };
            }

            if (button != null)
            {
                button.Click += (s, e) => this.Hide();
            }
            
            // Hide on close instead of exiting
            this.Closing += (s, e) => {
                e.Cancel = true;
                this.Hide();
            };
        }

        public void SetVolume(float volume)
        {
            var slider = this.FindControl<Slider>("VolumeSlider");
            if (slider != null)
            {
                slider.Value = volume * 100;
            }
        }
    }
}
