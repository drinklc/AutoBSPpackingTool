using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using static AutoBSPpackingTool.Util;
using System.Reflection;

namespace AutoBSPpackingTool
{
	public partial class Form2 : Form
	{
		public Form2()
		{
			InitializeComponent();
			
			checkedListBox2.Height = 4 + 15 * Clamp(Form1.games_info_default[0].available_settings.Length, 3, 5);
			ClientSize = new Size(ClientSize.Width, checkedListBox2.Bottom + 16);
		}

		public Form1 main_form;
		private bool setting_up_checkedlistboxes = true;

		private void Form2_Load(object sender, EventArgs e)
		{
			Text = main_form.Text+" - Settings";

			textBox1.Text = main_form.steam_path;

			checkedListBox1.Items.AddRange(Enum.GetValues(typeof(ExtraFiles)).Cast<ExtraFiles>().Select(item => item.GetTextField()).ToArray());
			checkedListBox2.Items.AddRange(Enum.GetValues(typeof(Settings)).Cast<Settings>().Select(item => item.GetTextField()).ToArray());

			//IgnoreNextCombobox1Event();
			SetupGamesCombobox();

			for(int i = 0;i < checkedListBox1.Items.Count;i++)
			{
				checkedListBox1.SetItemChecked(i, main_form.extra_files[i]);
			}
			for(int i = 0;i < checkedListBox2.Items.Count;i++)
			{
				checkedListBox2.SetItemChecked(i, main_form.settings[i]);
			}
			setting_up_checkedlistboxes = false;
		}

		private void Form2_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(ActiveControl is TextBox)
			{
				ActiveControl = null;
			}
		}

		void IgnoreNextCombobox1Event()
		{
			comboBox1.SelectedIndexChanged -= comboBox1_SelectedIndexChanged;
			comboBox1.SelectedIndexChanged += RetrieveOriginalCombobox1Event;
		}

		private void RetrieveOriginalCombobox1Event(object sender, EventArgs e)
		{
			comboBox1.SelectedIndexChanged -= RetrieveOriginalCombobox1Event;
			comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
		}

		public void SetupGamesCombobox(bool is_update = false)
		{
			comboBox1.Items.Clear();
			comboBox1.Items.AddRange(main_form.games_info.Select(item => item.game_folder).ToArray());
			if(is_update)IgnoreNextCombobox1Event();
			comboBox1.SelectedIndex = main_form.game;
			SetComboboxSize();

			if(is_update)
			{
				{
					List<int> checkboxes_off = new List<int>();
					List<int> checkboxes_on = new List<int>();
					for(int i = 0;i < checkedListBox1.Items.Count;i++)
					{
						if(!main_form.games_info[main_form.game].available_extra_files[i])
						{
							checkboxes_off.Add(i);
						}
						else if(main_form.games_info[main_form.game].available_extra_files[i] && !checkedListBox1.GetItemEnabled(i))
						{
							//if(i == (int)ExtraFiles.RadarImages && (!checkedListBox1.GetItemChecked((int)ExtraFiles.RadarInformation) && !checkboxes_on.Contains((int)ExtraFiles.RadarInformation)))continue;
							checkboxes_on.Add(i);
						}
					}
					for(int k = 0;k < checkboxes_off.Count;k++)
					{
						checkedListBox1.SetItemEnabled(checkboxes_off[k], false);
						checkedListBox1.SetItemChecked(checkboxes_off[k], false);
					}
					for(int k = 0;k < checkboxes_on.Count;k++)
					{
						checkedListBox1.SetItemEnabled(checkboxes_on[k], true);
						checkedListBox1.SetItemChecked(checkboxes_on[k], true);
					}
				}
				for(int i = 0;i < checkedListBox2.Items.Count;i++)
				{
					if(!main_form.games_info[main_form.game].available_settings[i])
					{
						checkedListBox2.SetItemEnabled(i, false);
						checkedListBox2.SetItemChecked(i, false);
					}
					else if(main_form.games_info[main_form.game].available_settings[i] && !checkedListBox2.GetItemEnabled(i))
					{
						checkedListBox2.SetItemEnabled(i, true);
						checkedListBox2.SetItemChecked(i, true);
					}
					if(i == (int)Settings.DetectCustomSearchPaths &&/* checkedListBox2.GetItemEnabled(i) &&*/ !main_form.games_info[main_form.game].assets_search_settings[(int)AssetsSearchSettings.AddExtraOwnVPKs] && !main_form.games_info[main_form.game].assets_search_settings[(int)AssetsSearchSettings.AddExtraOwnGameFolder])checkedListBox2.SetItemEnabled(i, false);
				}
			}
		}

		public void SetComboboxSize()
		{
			string[] games = comboBox1.Items.Cast<Object>().Select(item => item.ToString()).ToArray();
			int panel_size = games.Select(item => TextRenderer.MeasureText(item, comboBox1.Font).Width).Max() + 19 + 4 + button2.Size.Width;
			comboBox1.Size = new Size(Clamp(panel_size - 4 - button2.Size.Width, 19, ClientSize.Width - (16 * 2) - 4 - button2.Size.Width), comboBox1.Size.Height);
			comboBox1.Location = new Point(Math.Max(Round((ClientSize.Width - panel_size) / 2f), 16), comboBox1.Location.Y);
			button2.Location = new Point(comboBox1.Location.X + comboBox1.Size.Width + 4, button2.Location.Y);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			string new_path = null;
			if(Environment.OSVersion.Version >= new Version(6, 0)) //starting from Windows Vista
			{
				FolderPicker folder_browser = new FolderPicker{InitialDirectory = PathCombine(main_form.steam_path).Replace('/', '\\')}; //for some reason, InitialDirectory only works with \
				if(folder_browser.ShowDialog(Handle) == true)
				{
					if(folder_browser.ResultPath.StartsWith("::"))return;
					new_path = PathCombine(folder_browser.ResultPath);
				}
			}
			else
			{
				using(OpenFileDialog file_dialog = new OpenFileDialog{FileName = "Folder Selection", InitialDirectory = PathCombine(main_form.steam_path).Replace('/', '\\'), AddExtension = false, CheckFileExists = false, ValidateNames = false}) //for some reason, InitialDirectory only works with \
				{
					if(file_dialog.ShowDialog() == DialogResult.OK)
					{
						new_path = Path.GetDirectoryName(file_dialog.FileName);
					}
				}
			}
			if(new_path != null)
			{
				textBox1.Text = new_path;
				textBox1_Leave(textBox1, EventArgs.Empty);
			}
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			if(ActiveControl != textBox1)return;
			string replaced_string = ReplaceArray(textBox1.Text.Replace('\\', '/'), Path.GetInvalidFileNameChars().Except(new HashSet<char>{'/', '\\', ':'}), "");
			if(textBox1.Text != replaced_string)
			{
				int new_selection_start = textBox1.SelectionStart - (textBox1.TextLength - replaced_string.Length);
				textBox1.Text = replaced_string;
				textBox1.SelectionStart = new_selection_start;
				//return;
			}
		}

		private void textBox1_Leave(object sender, EventArgs e)
		{
			main_form.steam_path = textBox1.Text;
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			main_form.game = comboBox1.SelectedIndex;
			for(int i = 0;i < checkedListBox1.Items.Count;i++)
			{
				checkedListBox1.SetItemEnabled(i, main_form.games_info[main_form.game].available_extra_files[i]);
				checkedListBox1.SetItemChecked(i, main_form.games_info[main_form.game].available_extra_files[i]);
			}
			for(int i = 0;i < checkedListBox2.Items.Count;i++)
			{
				checkedListBox2.SetItemEnabled(i, main_form.games_info[main_form.game].available_settings[i] && (i != (int)Settings.DetectCustomSearchPaths || main_form.games_info[main_form.game].assets_search_settings[(int)AssetsSearchSettings.AddExtraOwnVPKs] || main_form.games_info[main_form.game].assets_search_settings[(int)AssetsSearchSettings.AddExtraOwnGameFolder]));
				checkedListBox2.SetItemChecked(i, main_form.games_info[main_form.game].available_settings[i]);
			}
			main_form.toolTip1.SetToolTip(main_form.button3, "Game: "+main_form.games_info[main_form.game].game_folder);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			Form3 game_configs_form = new Form3(){main_form = main_form, settings_form = this};
			game_configs_form.ShowDialog();
		}

		private void textBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)
			{
				e.SuppressKeyPress = true;
				ActiveControl = null;
			}
			else if(e.KeyCode == Keys.Escape)
			{
				e.SuppressKeyPress = true;
				textBox1.Text = main_form.steam_path;
				ActiveControl = null;
			}
		}

		private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			bool current_checked = e.NewValue == CheckState.Checked;
			if(!setting_up_checkedlistboxes)main_form.extra_files[e.Index] = current_checked;
			/*if(e.Index == (int)ExtraFiles.RadarInformation)
			{
				if(!current_checked)
				{
					checkedListBox1.SetItemChecked((int)ExtraFiles.RadarImages, false);
				}
				checkedListBox1.SetItemEnabled((int)ExtraFiles.RadarImages, main_form.games_info[main_form.game].available_extra_files[(int)ExtraFiles.RadarImages] && current_checked);
*/		}

		private void checkedListBox2_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			bool current_checked = e.NewValue == CheckState.Checked;
			if(!setting_up_checkedlistboxes)main_form.settings[e.Index] = current_checked;
		}

		private void checkedListBox1_Leave(object sender, EventArgs e)
		{
			checkedListBox1.ClearSelected();
		}

		private void checkedListBox2_Leave(object sender, EventArgs e)
		{
			checkedListBox2.ClearSelected();
		}
	}
}