using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Buzz.MachineInterface;
using BuzzGUI.Interfaces;
using BuzzGUI.Common;
using System.Windows.Media;

namespace WDE.Info
{
	[MachineDecl(Name = "Info View", ShortName = "Info", Author = "WDE", MaxTracks = 1)]
	public class InfoMachine : IBuzzMachine, INotifyPropertyChanged
	{
		IBuzzMachineHost host;
		CustomInfoWindow customInfoWindow;

		public InfoMachine(IBuzzMachineHost host)
		{
			this.host = host;
			Global.Buzz.Song.MachineAdded += Song_MachineAdded;
			Global.Buzz.Song.MachineRemoved += Song_MachineRemoved;
		}


        private void Song_MachineRemoved(IMachine obj)
        {
			if (host.Machine == obj)
			{
				Global.Buzz.Song.MachineAdded -= Song_MachineAdded;
				Global.Buzz.Song.MachineRemoved -= Song_MachineRemoved;

				if (customInfoWindow != null)
				{	
					customInfoWindow.Dispose();
				}
			}
		}

		private void Song_MachineAdded(IMachine obj)
		{
			if (host.Machine == obj)
			{
				if (!CustomInfoWindow.OneInstanceCreated)
				{
					customInfoWindow = new CustomInfoWindow();
					RTFBoxInfo rtfb = customInfoWindow.rTFBox;
					if (machineState.Text != null)
						rtfb.SetRTF(machineState.Text);

					Color col = machineState.Background;
					rtfb.GetRichTextBox().Background = new SolidColorBrush(col);
					col = machineState.Foreground;
					rtfb.GetRichTextBox().Foreground = new SolidColorBrush(col);
				}
			}
		}

		public void ImportFinished(IDictionary<string, string> machineNameMap)
        {
			//customInfoWindow.FocusBuzzWindow();
		}

		public static ResourceDictionary GetBuzzThemeResources()
		{
			ResourceDictionary skin = new ResourceDictionary();

			try
			{
				string selectedTheme = Global.Buzz.SelectedTheme == "<default>" ? "Default" : Global.Buzz.SelectedTheme;
				string skinPath = Global.BuzzPath + "\\Themes\\" + selectedTheme + "\\Gear\\Info\\RTFBoxInfo.xaml";

				skin.Source = new Uri(skinPath, UriKind.Absolute);
			}
			catch (Exception e)
			{
				Global.Buzz.DCWriteLine(e.Message);
				//string skinPath = Global.BuzzPath + "\\Themes\\Default\\SequenceEditor\\ToolBar.xaml";
				//skin.Source = new Uri(skinPath, UriKind.Absolute);
			}
			
			return skin;
		}

        [ParameterDecl(DefValue = false)]
		public bool Dummy { get; set; }

		internal static uint ColorToUInt(Color color)
		{
			unchecked
			{
				return (uint)((color.A << 24) | (color.R << 16) |
							  (color.G << 8) | (color.B << 0));
			}
		}

		internal static Color UIntToColor(uint color)
		{
			unchecked
			{
				byte a = (byte)(color >> 24);
				byte r = (byte)(color >> 16);
				byte g = (byte)(color >> 8);
				byte b = (byte)(color >> 0);
				return Color.FromArgb(a, r, g, b);
			}
		}

		// actual machine ends here. the stuff below demonstrates some other features of the api.

		public class State : INotifyPropertyChanged
		{
			public State()
			{
				Color bg = Global.Buzz.ThemeColors["IV BG"];
				Color fg = Global.Buzz.ThemeColors["IV Text"];
				if (bg == fg)
                {
					bg = Colors.White;
					fg = Colors.Black;
                }
				background = ColorToUInt(bg);
				foreground = ColorToUInt(fg);
			}	// NOTE: parameterless constructor is required by the xml serializer

			string text;
			public string Text 
			{
				get { return text; }
				set
				{
					text = value;
					if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Text"));
					// NOTE: the INotifyPropertyChanged stuff is only used for data binding in the GUI in this demo. it is not required by the serializer.
				}
			}
			
            public Color Background { get => UIntToColor(background); set => background = ColorToUInt(value); }
            public Color Foreground { get => UIntToColor(foreground); set => foreground = ColorToUInt(value); }
            

			uint background;
			uint foreground;


			public event PropertyChangedEventHandler PropertyChanged;
		}

		State machineState = new State();
		public State MachineState			// a property called 'MachineState' gets automatically saved in songs and presets
		{
			get
			{
				/*
				RichTextBox rtb = customInfoWindow.rTFBox.GetRichTextBox();
				string rtf;

				using (MemoryStream stream = new MemoryStream())
				{
					TextRange textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
					textRange.Save(stream, DataFormats.Rtf);
					stream.Seek(0, SeekOrigin.Begin);

					using (StreamReader reader = new StreamReader(stream))
					{
						rtf = reader.ReadToEnd();
					}
				}
				*/

				if (customInfoWindow != null)
				{
					machineState.Text = customInfoWindow.rTFBox.GetRTF();
					machineState.Background = ((SolidColorBrush)customInfoWindow.rTFBox.GetRichTextBox().Background).Color;
					machineState.Foreground = ((SolidColorBrush)customInfoWindow.rTFBox.GetRichTextBox().Foreground).Color;
				}
				return machineState;
			}
			set
			{
				machineState = value;
				if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("MachineState"));
			}
		}		
		
		public IEnumerable<IMenuItem> Commands
		{
			get
			{
				yield return new MenuItemVM() 
				{ 
					Text = "About...", 
					Command = new SimpleCommand()
					{
						CanExecuteDelegate = p => true,
						ExecuteDelegate = p => MessageBox.Show("Info View (C) 0.3 WDE 2021\n\nBased on WPF RichTextEditor with Toolbar by GregorPross\nhttps://www.codeproject.com/Articles/50139/WPF-RichTextEditor-with-Toolbar")
					}
				};
			}
		}

		internal void NotebookContentChanged()
		{
			// Notify buzz that something was changed
			// Hack, don't know how to call SetModifiedFlag();       
			int val = host.Machine.ParameterGroups[0].Parameters[0].GetValue(0);
			host.Machine.ParameterGroups[0].Parameters[0].SetValue(0, val);
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public class MachineGUIFactory : IMachineGUIFactory { public IMachineGUI CreateGUI(IMachineGUIHost host) { return new InfoGUI(); } }
	public class InfoGUI : UserControl, IMachineGUI
	{
		IMachine machine;
		InfoMachine infoMachine;
		
		public IMachine Machine
		{
			get { return machine; }
			set
			{
				if (machine != null)
				{
					// BindingOperations.ClearBinding(tb, TextBox.TextProperty);
				}

				machine = value;

				if (machine != null)
				{
					infoMachine = (InfoMachine)machine.ManagedMachine;
					// tb.SetBinding(TextBox.TextProperty, new Binding("MachineState.Text") { Source = infoMachine, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
				}
			}
		}
		
		public InfoGUI()
		{

		}
	}
}
