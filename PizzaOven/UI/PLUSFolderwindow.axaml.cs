using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PizzaOven.UI
{
    /// <summary>
    /// Interaction logic for PLUSFolderwindow.axaml
    /// </summary>
    public partial class PLUSFolderwindow : Window
    {
        public string _name;
        public bool _folder;
        public string directory = null;
        public string newName;
        public string loadout = null;

        public PLUSFolderwindow(string name, bool folder)
        {
            InitializeComponent();
            _folder = folder;
            
            if (!String.IsNullOrEmpty(name))
            {
                _name = name;
                NameBox.Text = name;
                Title = $"Edit Folder Name for {name}";
            }
            else if (_folder)
            {
                Title = "Create New Mod";
            }
            else
            {
                Title = "Create New Loadout";
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            NameBox?.Focus();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
        {
            EditFolderName();
            Close();
        }

        private void EditFolderName()
        {
            PLUSSavesystem.write_ini("Folder", _name, NameBox.Text);
        }
    }
}