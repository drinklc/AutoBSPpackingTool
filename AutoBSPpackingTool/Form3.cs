using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static AutoBSPpackingTool.Util;

namespace AutoBSPpackingTool
{
	public partial class Form3 : Form
	{
		public Form3()
		{
			InitializeComponent();

			button_expand_image.MakeTransparent();
			button_collapse_image.MakeTransparent();
			UpdateAdvancedSettings();
		}

		public Form1 main_form;
		public Form2 settings_form;
		private GameInfo start_game_info;
		private static Bitmap button_expand_image = new Bitmap(typeof(ThreadExceptionDialog), "down.bmp");
		private static Bitmap button_collapse_image = new Bitmap(typeof(ThreadExceptionDialog), "up.bmp");
		private bool setting_up_assets_search_order = false;

		private static Dictionary<string, AssetsSearchOrderOption> assets_search_option_names = new Dictionary<string, AssetsSearchOrderOption>(StringComparer.OrdinalIgnoreCase)
		{
			{AssetsSearchOrderOption.OwnVPKs.GetTextField(), AssetsSearchOrderOption.OwnVPKs},
			{AssetsSearchOrderOption.OwnGameFolder.GetTextField(), AssetsSearchOrderOption.OwnGameFolder},
			{AssetsSearchOrderOption.MountedFiles.GetTextField(), AssetsSearchOrderOption.MountedFiles},
			{AssetsSearchOrderOption.MountedVPKs.GetTextField(), AssetsSearchOrderOption.MountedVPKs},
			{AssetsSearchOrderOption.MountedFolders.GetTextField(), AssetsSearchOrderOption.MountedFolders}
		};

		private static GameInfo game_info_default = new GameInfo
		{
			game_folder = "game folder",
			game_root_folder = "game root folder",
			vpk_paths = new List<string>{},
			available_extra_files = Enumerable.Repeat(true, Form1.games_info_default[0].available_extra_files.Length).ToArray(),
			available_settings = Enumerable.Repeat(true, Form1.games_info_default[0].available_settings.Length).ToArray(),
			assets_search_settings = new bool[]{false, true, true, true},
			assets_search_order = new List<AssetsSearchOrderOption>{AssetsSearchOrderOption.OwnVPKs, AssetsSearchOrderOption.OwnGameFolder, AssetsSearchOrderOption.MountedFiles}
		};

		private bool advanced_settings_shown = false;

		private void Form3_Load(object sender, EventArgs e)
		{
			Text = main_form.Text+" - Game Configurations";

			start_game_info = main_form.games_info[main_form.game];

			checkedListBox1.Items.AddRange(settings_form.checkedListBox1.Items);
			checkedListBox2.Items.AddRange(settings_form.checkedListBox2.Items);
			checkedListBox3.Items.AddRange(Enum.GetValues(typeof(AssetsSearchSettings)).Cast<AssetsSearchSettings>().Select(item => item.GetTextField()).ToArray());

			SetupListBox1(main_form.game);
		}

		private void Form3_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(ActiveControl is TextBox textbox)
			{
				ActiveControl = null;
			}
			bool reload = main_form.game > main_form.games_info.Count - 1 || !start_game_info.game_folder.EqualsCI(main_form.games_info[main_form.game].game_folder);
			main_form.game = Math.Min(main_form.game, main_form.games_info.Count - 1);
			settings_form.SetupGamesCombobox(!reload);
		}

		void SetupListBox1(int index)
		{
			listBox1.Items.Clear();
			for(int i = 0;i < main_form.games_info.Count;i++)
			{
				listBox1.Items.Add(main_form.games_info[i].game_folder);
			}
			listBox1.SelectedIndex = index;
		}

		void SetupListBox2(int index)
		{
			List<string> vpk_paths = main_form.games_info[listBox1.SelectedIndex].vpk_paths;
			listBox2.Items.Clear();
			for(int i = 0;i < vpk_paths.Count;i++)
			{
				listBox2.Items.Add(vpk_paths[i]);
			}
			if(vpk_paths.Count > 0)
			{
				listBox2.SelectedIndex = index;
			}
			textBox3.Enabled = vpk_paths.Count > 0;
			textBox3.Text = vpk_paths.Count > 0 ? vpk_paths[index] : "";
			button9.Enabled = vpk_paths.Count > 0;
			button5.Enabled = vpk_paths.Count > 0;
			UpdateListBox2ResetButton();
		}

		void UpdateCheckedListBoxes(bool first = true, bool second = true)
		{
			if(first)
			{
				for(int i = 0;i < settings_form.checkedListBox1.Items.Count;i++)
				{
					checkedListBox1.SetItemChecked(i, main_form.games_info[listBox1.SelectedIndex].available_extra_files[i]);
				}
			}
			if(second)
			{
				for(int i = 0;i < settings_form.checkedListBox2.Items.Count;i++)
				{
					checkedListBox2.SetItemChecked(i, main_form.games_info[listBox1.SelectedIndex].available_settings[i]);
				}
			}
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool not_default = listBox1.SelectedIndex >= Form1.games_info_default.Count;
			button2.Enabled = listBox1.SelectedIndex >= Form1.games_info_default.Count;
			textBox1.Text = main_form.games_info[listBox1.SelectedIndex].game_folder;
			textBox2.Text = main_form.games_info[listBox1.SelectedIndex].game_root_folder;
			textBox1.Enabled = not_default;
			textBox2.Enabled = not_default;
			SetupListBox2(0);
			setting_up_assets_search_order = true;
			UpdateCheckedListBoxes();
			UpdateAssetsSearchOrderAndSettings();
			setting_up_assets_search_order = false;
			CheckDetectSearchPathsAvailable();
		}

		private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			textBox3.Text = main_form.games_info[listBox1.SelectedIndex].vpk_paths[listBox2.SelectedIndex];
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			_textBox1_TextChanged(sender, e);
		}

		private void _textBox1_TextChanged(object sender, EventArgs e, bool ignore_not_active = false)
		{
			if(!ignore_not_active && ActiveControl != textBox1)return;
			string replaced_string = ReplaceArray(textBox1.Text, Path.GetInvalidFileNameChars(), "");
			if(textBox1.Text != replaced_string)
			{
				int new_selection_start = textBox1.SelectionStart - (textBox1.TextLength - replaced_string.Length);
				textBox1.Text = replaced_string;
				textBox1.SelectionStart = new_selection_start;
				return;
			}
			listBox1.SelectedIndexChanged -= listBox1_SelectedIndexChanged;
			listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
			listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
			CheckSaveButtonEnabled();
		}

		private void textBox1_Leave(object sender, EventArgs e)
		{
			if(textBox1.TextLength == 0)
			{
				textBox1.Text = main_form.games_info[listBox1.SelectedIndex].game_folder;
				_textBox1_TextChanged(textBox1, EventArgs.Empty, true);
				return;
			}
			textBox1.Text = FixGameFolderName(textBox1.Text, listBox1.SelectedIndex);
			_textBox1_TextChanged(textBox1, EventArgs.Empty, true);
			GameInfo new_game_info = main_form.games_info[listBox1.SelectedIndex];
			new_game_info.game_folder = textBox1.Text;
			main_form.games_info[listBox1.SelectedIndex] = new_game_info;
		}

		private void textBox2_TextChanged(object sender, EventArgs e)
		{
			_textBox2_TextChanged(sender, e);
		}

		private void _textBox2_TextChanged(object sender, EventArgs e, bool ignore_not_active = false)
		{
			if(!ignore_not_active && ActiveControl != textBox2)return;
			string replaced_string = ReplaceArray(textBox2.Text, Path.GetInvalidFileNameChars(), "");
			if(textBox2.Text != replaced_string)
			{
				int new_selection_start = textBox2.SelectionStart - (textBox2.TextLength - replaced_string.Length);
				textBox2.Text = replaced_string;
				textBox2.SelectionStart = new_selection_start;
				return;
			}
			CheckSaveButtonEnabled();
		}

		private void textBox2_Leave(object sender, EventArgs e)
		{
			if(textBox2.TextLength == 0)
			{
				textBox2.Text = main_form.games_info[listBox1.SelectedIndex].game_root_folder;
				_textBox2_TextChanged(textBox2, EventArgs.Empty, true);
				return;
			}
			GameInfo new_game_info = main_form.games_info[listBox1.SelectedIndex];
			new_game_info.game_root_folder = textBox2.Text;
			main_form.games_info[listBox1.SelectedIndex] = new_game_info;
		}

		private void textBox3_TextChanged(object sender, EventArgs e)
		{
			_textBox3_TextChanged(sender, e);
		}

		private void _textBox3_TextChanged(object sender, EventArgs e, bool ignore_not_active = false)
		{
			if(!ignore_not_active && ActiveControl != textBox3)return;
			string replaced_string = ReplaceArray(textBox3.Text.Replace('\\', '/'), Path.GetInvalidFileNameChars().Except(new HashSet<char>{'/', '\\', ':'}), "");
			if(textBox3.Text != replaced_string)
			{
				int new_selection_start = textBox3.SelectionStart - (textBox3.TextLength - replaced_string.Length);
				textBox3.Text = replaced_string;
				textBox3.SelectionStart = new_selection_start;
				return;
			}
			listBox2.SelectedIndexChanged -= listBox2_SelectedIndexChanged;
			listBox2.Items[listBox2.SelectedIndex] = textBox3.Text;
			listBox2.SelectedIndexChanged += listBox2_SelectedIndexChanged;
			UpdateListBox2ResetButton();
			CheckSaveButtonEnabled();
		}

		private void textBox3_Leave(object sender, EventArgs e)
		{
			List<string> vpk_paths = main_form.games_info[listBox1.SelectedIndex].vpk_paths;
			if(textBox3.TextLength == 0)
			{
				textBox3.Text = vpk_paths[listBox2.SelectedIndex];
				_textBox3_TextChanged(textBox3, EventArgs.Empty, true);
				return;
			}
			vpk_paths[listBox2.SelectedIndex] = textBox3.Text;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			main_form.games_info.Add(new GameInfo
			{
				game_folder = FixGameFolderName(game_info_default.game_folder),
				game_root_folder = game_info_default.game_root_folder,
				vpk_paths = game_info_default.vpk_paths.ToList(),
				available_extra_files = game_info_default.available_extra_files.ToArray(),
				available_settings = game_info_default.available_settings.ToArray(),
				assets_search_settings = game_info_default.assets_search_settings.ToArray(),
				assets_search_order = game_info_default.assets_search_order.ToList(),
				config_priority = GetUnixTimeMilliseconds()
			});
			SetupListBox1(listBox1.Items.Count);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			string game_cfg_path = GetGameCfgPath(listBox1.SelectedIndex);
			if(game_cfg_path == null || MessageBox.Show("Are you sure you want to delete this game configuration?\nThis will also remove the game configuration file from your computer.", main_form.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
			{
				main_form.games_info.RemoveAt(listBox1.SelectedIndex);
				SetupListBox1(Clamp(listBox1.SelectedIndex - 1, 0, listBox1.Items.Count - 2));
				if(game_cfg_path != null)File.Delete(game_cfg_path);
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			string cfg_path = GetGameCfgPath(listBox1.SelectedIndex);
			if(!Directory.Exists(Form1.cfgs_path))Directory.CreateDirectory(Form1.cfgs_path);
			if(listBox1.SelectedIndex < Form1.games_info_default.Count && cfg_path != null && AreConfigsEqual(main_form.games_info[listBox1.SelectedIndex], Form1.games_info_default[listBox1.SelectedIndex]))
			{
				File.Delete(cfg_path);
			}
			else
			{
				File.WriteAllText(cfg_path ?? PathCombine(Form1.cfgs_path, main_form.games_info[listBox1.SelectedIndex].game_folder+".txt"), main_form.GetGameCfg(listBox1.SelectedIndex));
			}
			button3.Enabled = false;

			ActiveControl = null;
		}

		private void button4_Click(object sender, EventArgs e)
		{
			main_form.games_info[listBox1.SelectedIndex].vpk_paths.Add("new_dir.vpk");
			SetupListBox2(listBox2.Items.Count);
			CheckSaveButtonEnabled();
		}

		private void button5_Click(object sender, EventArgs e)
		{
			main_form.games_info[listBox1.SelectedIndex].vpk_paths.RemoveAt(listBox2.SelectedIndex);
			SetupListBox2(Clamp(listBox2.SelectedIndex - 1, 0, listBox2.Items.Count - 2));
			CheckSaveButtonEnabled();
		}

		private void button6_Click(object sender, EventArgs e)
		{
			GameInfo new_game_info = main_form.games_info[listBox1.SelectedIndex];
			new_game_info.vpk_paths = (listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].vpk_paths : game_info_default.vpk_paths).ToList();
			main_form.games_info[listBox1.SelectedIndex] = new_game_info;
			SetupListBox2(0);
			CheckSaveButtonEnabled();

			ActiveControl = null;
		}

		private void button7_Click(object sender, EventArgs e)
		{
			GameInfo new_game_info = main_form.games_info[listBox1.SelectedIndex];
			new_game_info.available_extra_files = (listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].available_extra_files : game_info_default.available_extra_files).ToArray();
			main_form.games_info[listBox1.SelectedIndex] = new_game_info;
			UpdateCheckedListBoxes(second: false);

			ActiveControl = null;
		}

		private void button8_Click(object sender, EventArgs e)
		{
			GameInfo new_game_info = main_form.games_info[listBox1.SelectedIndex];
			new_game_info.available_settings = (listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].available_settings : game_info_default.available_settings).ToArray();
			main_form.games_info[listBox1.SelectedIndex] = new_game_info;
			UpdateCheckedListBoxes(first: false);

			ActiveControl = null;
		}

		private void button9_Click(object sender, EventArgs e)
		{
			main_form.FindLibraryDirectory(main_form.games_info[listBox1.SelectedIndex].game_folder, main_form.games_info[listBox1.SelectedIndex].game_root_folder, out string library_path);
			string initial_directory = Path.GetDirectoryName(library_path == null || IsFullPath(textBox3.Text) ? textBox3.Text : PathCombine(library_path, "steamapps/common", main_form.games_info[listBox1.SelectedIndex].game_folder, textBox3.Text));
			using(OpenFileDialog file_dialog = new OpenFileDialog{FileName = "VPK file", Filter = "Dir VPK Files|*_dir.VPK|VPK Files|*.VPK", InitialDirectory = library_path == null || Directory.Exists(initial_directory) ? initial_directory : PathCombine(library_path, "steamapps/common", main_form.games_info[listBox1.SelectedIndex].game_folder).Replace('/', '\\')}) //for some reason, InitialDirectory only works with \
			{
				if(file_dialog.ShowDialog() == DialogResult.OK)
				{
					string file_path = PathCombine(file_dialog.FileName);
					textBox3.Text = library_path == null || IsVpkExternal(file_path, library_path, main_form.games_info[listBox1.SelectedIndex].game_folder) ? file_path : file_path.Remove(0, PathCombine(library_path, "steamapps/common", main_form.games_info[listBox1.SelectedIndex].game_folder).Length + 1);
					_textBox3_TextChanged(textBox3, EventArgs.Empty, true);
					textBox3_Leave(textBox3, EventArgs.Empty);
				}
			}
		}

		private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			bool current_checked = e.NewValue == CheckState.Checked;
			main_form.games_info[listBox1.SelectedIndex].available_extra_files[e.Index] = current_checked;
			/*if(e.Index == (int)ExtraFiles.RadarInformation)
			{
				if(!current_checked)
				{
					checkedListBox1.SetItemChecked((int)ExtraFiles.RadarImages, false);
				}
				checkedListBox1.SetItemEnabled((int)ExtraFiles.RadarImages, current_checked);
			}*/
			UpdateCheckedListBox1ResetButton();
			CheckSaveButtonEnabled();
		}

		private void checkedListBox2_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			bool current_checked = e.NewValue == CheckState.Checked;
			main_form.games_info[listBox1.SelectedIndex].available_settings[e.Index] = current_checked;
			if(e.Index == (int)Settings.DetectCustomSearchPaths)
			{
				if(e.NewValue != e.CurrentValue && !setting_up_assets_search_order)
				{
					List<AssetsSearchOrderOption> assets_search_order = main_form.games_info[listBox1.SelectedIndex].assets_search_order;
					bool[] assets_search_settings = main_form.games_info[listBox1.SelectedIndex].assets_search_settings;
					if(current_checked)
					{
						if(assets_search_settings[(int)AssetsSearchSettings.SeparateMountedFoldersAndVPKs])
						{
							assets_search_order.Add(AssetsSearchOrderOption.MountedVPKs);
							assets_search_order.Add(AssetsSearchOrderOption.MountedFolders);
						}
						else
						{
							assets_search_order.Add(AssetsSearchOrderOption.MountedFiles);
						}
					}
					else
					{
						if(!assets_search_settings[(int)AssetsSearchSettings.AddExtraOwnVPKs] && !assets_search_settings[(int)AssetsSearchSettings.AddExtraOwnGameFolder])
						{
							checkedListBox3.SetItemChecked((int)AssetsSearchSettings.AddExtraOwnVPKs, true);
							checkedListBox3.SetItemChecked((int)AssetsSearchSettings.AddExtraOwnGameFolder, true);
						}
						if(assets_search_settings[(int)AssetsSearchSettings.SeparateMountedFoldersAndVPKs])
						{
							assets_search_order.Remove(AssetsSearchOrderOption.MountedVPKs);
							assets_search_order.Remove(AssetsSearchOrderOption.MountedFolders);
						}
						else
						{
							assets_search_order.Remove(AssetsSearchOrderOption.MountedFiles);
						}
					}
					UpdateAssetsSearchOrderAndSettings();
				}
				checkedListBox3.SetItemEnabled((int)AssetsSearchSettings.SeparateMountedFoldersAndVPKs, current_checked);
			}
			UpdateCheckedListBox2ResetButton();
			CheckSaveButtonEnabled();
		}

		void UpdateListBox2ResetButton()
		{
			button6.Enabled = !Enumerable.SequenceEqual(main_form.games_info[listBox1.SelectedIndex].vpk_paths.Select((item, index) => index != listBox2.SelectedIndex ? item : textBox3.Text), listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].vpk_paths : game_info_default.vpk_paths); //this code (Enumerable.SequenceEqual's first argument) is used so that the "Reset" button is updated in real time, even though the original value is changed when the textbox loses focus
		}

		void UpdateCheckedListBox1ResetButton()
		{
			button7.Enabled = !Enumerable.SequenceEqual(main_form.games_info[listBox1.SelectedIndex].available_extra_files, listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].available_extra_files : game_info_default.available_extra_files);
		}

		void UpdateCheckedListBox2ResetButton()
		{
			button8.Enabled = !Enumerable.SequenceEqual(main_form.games_info[listBox1.SelectedIndex].available_settings, listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].available_settings : game_info_default.available_settings);
		}

		void UpdateAssetsSearchSettingsAndOrderResetButton()
		{
			button11.Enabled = !Enumerable.SequenceEqual(main_form.games_info[listBox1.SelectedIndex].assets_search_settings, listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].assets_search_settings : game_info_default.assets_search_settings) || !Enumerable.SequenceEqual(main_form.games_info[listBox1.SelectedIndex].assets_search_order, listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].assets_search_order : game_info_default.assets_search_order);
		}

		void CheckSaveButtonEnabled()
		{
			string game_cfg_path = GetGameCfgPath(listBox1.SelectedIndex);
			GameInfo current_game_info = main_form.games_info[listBox1.SelectedIndex]; //several values are overridden here so that the "Save" button is updated in real time, even though the original values are changed when a textbox loses focus
			current_game_info.game_folder = textBox1.Text;
			current_game_info.game_root_folder = textBox2.Text;
			current_game_info.vpk_paths = current_game_info.vpk_paths.Select((item, index) => index != listBox2.SelectedIndex ? item : textBox3.Text).ToList();
			button3.Enabled =
				game_cfg_path == null
				? listBox1.SelectedIndex >= Form1.games_info_default.Count || !AreConfigsEqual(current_game_info, Form1.games_info_default[listBox1.SelectedIndex])
				: File.ReadAllText(game_cfg_path) != main_form.GetGameCfg(listBox1.SelectedIndex, game_folder_override: textBox1.Text, root_folder_override: textBox2.Text, vpks_override: string.Join("|", current_game_info.vpk_paths));
		}

		string GetGameCfgPath(int index)
		{
			if(!Directory.Exists(Form1.cfgs_path))return null;
			foreach(string game_cfg in Directory.EnumerateFiles(Form1.cfgs_path, "*.txt", SearchOption.TopDirectoryOnly))
			{
				DictionaryList<string, string> keyvalues = GetBlockData(new ContentInfo{file_path = game_cfg}, "game_config", parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues;
				if(keyvalues == null)continue;
				if(!keyvalues.TryGetValue("game_folder", out string game_folder))continue;
				if(!keyvalues.TryGetValue("root_folder", out string root_folder))continue;

				if(game_folder.EqualsCI(main_form.games_info[index].game_folder) && root_folder.EqualsCI(main_form.games_info[index].game_root_folder))
				{
					return game_cfg;
				}
			}
			return null;
		}

		bool AreConfigsEqual(GameInfo game_info1, GameInfo game_info2)
		{
			return
				game_info1.game_folder == game_info2.game_folder
				&& game_info1.game_root_folder == game_info2.game_root_folder
				&& game_info1.vpk_paths.SequenceEqual(game_info2.vpk_paths)
				&& game_info1.available_extra_files.SequenceEqual(game_info2.available_extra_files)
				&& game_info1.available_settings.SequenceEqual(game_info2.available_settings)
				&& game_info1.assets_search_settings.SequenceEqual(game_info2.assets_search_settings)
				&& game_info1.assets_search_order.SequenceEqual(game_info2.assets_search_order);
		}

		string FixGameFolderName(string game_folder, int current_index = -1)
		{
			while(main_form.games_info.Select((item, index) => new {item, index}).Any(item => item.index != current_index && item.item.game_folder.EqualsCI(game_folder)))
			{
				game_folder += '_';
			}
			return game_folder;
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
				textBox1.Text = main_form.games_info[listBox1.SelectedIndex].game_folder;
				ActiveControl = null;
			}
		}

		private void textBox2_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)
			{
				e.SuppressKeyPress = true;
				ActiveControl = null;
			}
			else if(e.KeyCode == Keys.Escape)
			{
				e.SuppressKeyPress = true;
				textBox2.Text = main_form.games_info[listBox1.SelectedIndex].game_root_folder;
				ActiveControl = null;
			}
		}

		private void textBox3_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)
			{
				e.SuppressKeyPress = true;
				ActiveControl = null;
			}
			else if(e.KeyCode == Keys.Escape)
			{
				e.SuppressKeyPress = true;
				textBox3.Text = main_form.games_info[listBox1.SelectedIndex].vpk_paths[listBox2.SelectedIndex];
				ActiveControl = null;
			}
		}

		private void button10_Click(object sender, EventArgs e)
		{
			advanced_settings_shown = !advanced_settings_shown;
			UpdateAdvancedSettings();
		}

		void UpdateAdvancedSettings()
		{
			button10.Image = advanced_settings_shown ? button_collapse_image : button_expand_image;
			if(advanced_settings_shown)
			{
				ClientSize = new Size(ClientSize.Width, button11.Bottom - 1 + 8);
			}
			else
			{
				ClientSize = new Size(ClientSize.Width, button10.Bottom - 1 + 8);
			}
		}

		private void checkedListBox3_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			bool current_checked = e.NewValue == CheckState.Checked;
			main_form.games_info[listBox1.SelectedIndex].assets_search_settings[e.Index] = current_checked;
			if(e.NewValue != e.CurrentValue && !setting_up_assets_search_order)
			{
				List<AssetsSearchOrderOption> assets_search_order = main_form.games_info[listBox1.SelectedIndex].assets_search_order;
				if(e.Index == (int)AssetsSearchSettings.SeparateMountedFoldersAndVPKs)
				{
					if(current_checked)
					{
						int index = assets_search_order.IndexOf(AssetsSearchOrderOption.MountedFiles);
						assets_search_order[index] = AssetsSearchOrderOption.MountedVPKs;
						assets_search_order.Insert(index + 1, AssetsSearchOrderOption.MountedFolders);
					}
					else
					{
						assets_search_order.Remove(AssetsSearchOrderOption.MountedFolders);
						assets_search_order[assets_search_order.IndexOf(AssetsSearchOrderOption.MountedVPKs)] = AssetsSearchOrderOption.MountedFiles;
					}
					UpdateAssetsSearchOrderList();
				}
				else if(e.Index == (int)AssetsSearchSettings.AddExtraOwnVPKs)
				{
					if(current_checked)
					{
						assets_search_order.Add(AssetsSearchOrderOption.OwnVPKs);
					}
					else
					{
						assets_search_order.Remove(AssetsSearchOrderOption.OwnVPKs);
					}
					if(!CheckDetectSearchPathsAvailable())UpdateAssetsSearchOrderList();
				}
				else if(e.Index == (int)AssetsSearchSettings.AddExtraOwnGameFolder)
				{
					if(current_checked)
					{
						assets_search_order.Add(AssetsSearchOrderOption.OwnGameFolder);
					}
					else
					{
						assets_search_order.Remove(AssetsSearchOrderOption.OwnGameFolder);
					}
					if(!CheckDetectSearchPathsAvailable())UpdateAssetsSearchOrderList();
				}
			}
			UpdateAssetsSearchSettingsAndOrderResetButton();
			CheckSaveButtonEnabled();
		}

		bool CheckDetectSearchPathsAvailable()
		{
			bool detect_search_paths_available = main_form.games_info[listBox1.SelectedIndex].assets_search_settings[(int)AssetsSearchSettings.AddExtraOwnVPKs] || main_form.games_info[listBox1.SelectedIndex].assets_search_settings[(int)AssetsSearchSettings.AddExtraOwnGameFolder];
			bool change_checked = !detect_search_paths_available && !main_form.games_info[listBox1.SelectedIndex].available_settings[(int)Settings.DetectCustomSearchPaths];
			if(change_checked)checkedListBox2.SetItemChecked((int)Settings.DetectCustomSearchPaths, true);
			checkedListBox2.SetItemEnabled((int)Settings.DetectCustomSearchPaths, detect_search_paths_available);
			return change_checked;
		}

		private void listBox3_ItemReordered(object sender, EventArgs e)
		{
			for(int i = 0;i < listBox3.Items.Count;i++)
			{
				main_form.games_info[listBox1.SelectedIndex].assets_search_order[i] = assets_search_option_names[listBox3.Items[i].ToString()];
			}
			UpdateAssetsSearchSettingsAndOrderResetButton();
			CheckSaveButtonEnabled();
		}

		void UpdateAssetsSearchOrderAndSettings()
		{
			for(int i = 0;i < main_form.games_info[listBox1.SelectedIndex].assets_search_settings.Length;i++)
			{
				checkedListBox3.SetItemChecked(i, main_form.games_info[listBox1.SelectedIndex].assets_search_settings[i]);
			}
			UpdateAssetsSearchOrderList();
		}

		void UpdateAssetsSearchOrderList()
		{
			listBox3.Items.Clear();
			for(int i = 0;i < main_form.games_info[listBox1.SelectedIndex].assets_search_order.Count;i++)
			{
				listBox3.Items.Add(main_form.games_info[listBox1.SelectedIndex].assets_search_order[i].GetTextField());
			}
		}

		private void listBox2_DragEnter(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void listBox2_DragDrop(object sender, DragEventArgs e)
		{
			if(ActiveControl is TextBox textbox)
			{
				ActiveControl = null;
			}
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if(files.Length == 1 && Directory.Exists(files[0]))
			{
				files = Directory.GetFiles(files[0], "*_dir.vpk", SearchOption.AllDirectories);
			}
			files = files.Where(item => Path.GetExtension(item).EqualsCI(".vpk")).OrderBy(item => Path.GetFileNameWithoutExtension(item)).ToArray();
			if(files.Length > 0)
			{
				main_form.FindLibraryDirectory(main_form.games_info[listBox1.SelectedIndex].game_folder, main_form.games_info[listBox1.SelectedIndex].game_root_folder, out string library_path);
				for(int i = 0;i < files.Length;i++)
				{
					files[i] = PathCombine(files[i]);
					main_form.games_info[listBox1.SelectedIndex].vpk_paths.Add(library_path == null || IsVpkExternal(files[i], library_path, main_form.games_info[listBox1.SelectedIndex].game_folder) ? files[i] : files[i].Remove(0, PathCombine(library_path, "steamapps/common", main_form.games_info[listBox1.SelectedIndex].game_folder).Length + 1));
				}
				SetupListBox2(main_form.games_info[listBox1.SelectedIndex].vpk_paths.Count - 1);
			}
		}

		private void button11_Click(object sender, EventArgs e)
		{
			GameInfo new_game_info = main_form.games_info[listBox1.SelectedIndex];
			new_game_info.assets_search_settings = (listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].assets_search_settings : game_info_default.assets_search_settings).ToArray();
			new_game_info.assets_search_order = (listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].assets_search_order : game_info_default.assets_search_order).ToList();
			main_form.games_info[listBox1.SelectedIndex] = new_game_info;
			setting_up_assets_search_order = true;
			if(main_form.games_info[listBox1.SelectedIndex].available_settings[(int)Settings.DetectCustomSearchPaths] != (listBox1.SelectedIndex < Form1.games_info_default.Count ? Form1.games_info_default[listBox1.SelectedIndex].available_settings[(int)Settings.DetectCustomSearchPaths] : game_info_default.available_settings[(int)Settings.DetectCustomSearchPaths]))
			{
				checkedListBox2.SetItemChecked((int)Settings.DetectCustomSearchPaths, !main_form.games_info[listBox1.SelectedIndex].available_settings[(int)Settings.DetectCustomSearchPaths]);
			}
			UpdateAssetsSearchOrderAndSettings();
			setting_up_assets_search_order = false;
			CheckDetectSearchPathsAvailable();

			ActiveControl = null;
		}

		private void checkedListBox1_Leave(object sender, EventArgs e)
		{
			checkedListBox1.ClearSelected();
		}

		private void checkedListBox2_Leave(object sender, EventArgs e)
		{
			checkedListBox2.ClearSelected();
		}

		private void checkedListBox3_Leave(object sender, EventArgs e)
		{
			checkedListBox3.ClearSelected();
		}

		private void listBox3_Leave(object sender, EventArgs e)
		{
			listBox3.ClearSelected();
		}
	}
}