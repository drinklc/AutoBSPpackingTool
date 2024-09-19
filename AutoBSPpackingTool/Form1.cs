using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using static AutoBSPpackingTool.Util;
using static BSPZIP.BSPZIP;
using static VPK.VPK;

namespace AutoBSPpackingTool
{
	public partial class Form1 : Form
	{
		public Form1(string[] args)
		{
			if(!Debugger.IsAttached) //sets exception messages to English for users and leaves them localized for developers
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			}
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleUnhandledException((Exception)e.ExceptionObject);
			Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);

			//cmd = args.Select((item, index) => new {item, index}).Any(item => item.index < args.Length - 1 && ((new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--vmf", "-vmf"}.Contains(item.item) && IsFullPath(args[item.index + 1]) && Path.GetExtension(args[item.index + 1]).EqualsCI(".vmf")) || (new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--bsp", "-bsp"}.Contains(item.item) && IsFullPath(args[item.index + 1]) && Path.GetExtension(args[item.index + 1]).EqualsCI(".bsp"))));
			cmd = args.Any(item => new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--vmf", "-vmf", "--bsp", "-bsp"}.Contains(item));
			if(!cmd)InitializeComponent();
			else
            {
				components = new System.ComponentModel.Container();
				System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
				Icon = (Icon)resources.GetObject("$this.Icon");
				Text = "AutoBSPpackingTool";
            }
            {
				notifyIcon = new NotifyIcon(components)
				{
					Text = Text,
					Icon = Icon,
					Visible = true
				};
				void RestoreFocus()
				{
					PostMessage(Handle, WM_SYSCOMMAND, SC_RESTORE, 0);
					SetForegroundWindow(Handle);
				}
				notifyIcon.BalloonTipClicked += (sender, e) => RestoreFocus();
				notifyIcon.Click += (sender, e) => {if(!cmd)RestoreFocus();};
            }
			Visible = !cmd;
			VoidLoad();
			DetectSteamDirectory();
			if(cmd)AttachConsole(ATTACH_PARENT_PROCESS);

			string game_directory = null;
			for(int i = 0;i < args.Length;i++)
			{
				if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--addcfg", "-addcfg"}.Contains(args[i])) //game configs must be added before game definition in the next 'for' loop
				{
					string fixed_path = i < args.Length - 1 ? PathCombine(args[i + 1]) : null;
					if(i == args.Length - 1)
					{
						Log("Missing path argument for \'"+args[i]+"\' parameter", LogType.Warning);
						continue;
					}
					else if(fixed_path == null || !IsFullPath(fixed_path))
					{
						Log("Invalid path specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					else if(!Path.GetExtension(fixed_path).EqualsCI(".txt"))
					{
						Log("Improper file extension specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					else if(!File.Exists(fixed_path))
					{
						Log("Could not find file specified in \'"+args[i]+"\' parameter", LogType.Warning);
					}
					else
					{
						AddGameCfg(fixed_path);
					}
					i++;
				}
			}

			games_info.Sort(games_info_default.Count, games_info.Count - games_info_default.Count, new GameInfoPriorityComparer());

			for(int i = 0;i < args.Length;i++)
			{
				if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--vmf", "-vmf"}.Contains(args[i]))
				{
					string fixed_path = i < args.Length - 1 ? PathCombine(args[i + 1]) : null;
					if(i == args.Length - 1)
					{
						Log("Missing path argument for \'"+args[i]+"\' parameter", LogType.Warning);
						continue;
					}
					else if(fixed_path == null || !IsFullPath(fixed_path))
					{
						Log("Invalid path specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					else if(!Path.GetExtension(fixed_path).EqualsCI(".vmf"))
					{
						Log("Improper file extension specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					else
					{
						input_vmf = fixed_path;
						vmf_name = Path.GetFileNameWithoutExtension(fixed_path);
					}
					i++;
				}
				else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--bsp", "-bsp"}.Contains(args[i]))
				{
					string fixed_path = i < args.Length - 1 ? PathCombine(args[i + 1]) : null;
					if(i == args.Length - 1)
					{
						Log("Missing path argument for \'"+args[i]+"\' parameter", LogType.Warning);
						continue;
					}
					else if(fixed_path == null || !IsFullPath(fixed_path))
					{
						Log("Invalid path specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					else if(!Path.GetExtension(fixed_path).EqualsCI(".bsp"))
					{
						Log("Improper file extension specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					else
					{
						input_bsp = fixed_path;
					}
					i++;
				}
				else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--game", "-game"}.Contains(args[i]))
				{
					if(int.TryParse(args[i + 1], out int game_index))
					{
						if(game_index >= 0 && game_index < games_info.Count)
						{
							game = game_index;
						}
						else
						{
							Log("Invalid index specified for \'"+args[i]+"\' parameter", LogType.Warning);
						}
					}
					else
					{
						string arg = null;
						if(Directory.Exists(args[i + 1]))
						{
							try
							{
								arg = Path.GetFileName(args[i + 1]);
								game_directory = args[i + 1];
							}
							catch{}
						}
						else
						{
							arg = args[i + 1];
						}
						game_index = arg != null ? games_info.FindIndex(item => item.game_root_folder.EqualsCI(arg)) : -1;
						if(game_index != -1)
						{
							game = game_index;
						}
						else
						{
							Log("Could not find a corresponding configuration for \'"+args[i]+"\' parameter", LogType.Warning);
						}
					}
					i++;
				}
				else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--cachedir", "-cachedir"}.Contains(args[i]))
				{
					string fixed_path = i < args.Length - 1 ? PathCombine(args[i + 1]) : null;
					if(i == args.Length - 1)
					{
						Log("Missing path argument for \'"+args[i]+"\' parameter", LogType.Warning);
						continue;
					}
					else if(fixed_path == null || !Directory.Exists(fixed_path))
					{
						Log("Invalid path specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					else
					{
						cache_path = fixed_path;
					}
					i++;
				}
				else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--gameinfo", "-gameinfo"}.Contains(args[i]))
				{
					string fixed_path = i < args.Length - 1 ? PathCombine(args[i + 1]) : null;
					if(i == args.Length - 1)
					{
						Log("Missing path argument for \'"+args[i]+"\' parameter", LogType.Warning);
						continue;
					}
					else if(fixed_path == null || !IsFullPath(fixed_path))
					{
						Log("Invalid path specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					/*else if(!Path.GetExtension(fixed_path).EqualsCI(".txt"))
					{
						Log("Improper file extension specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}*/
					else
					{
						gameinfo_path = fixed_path;
					}
					i++;
				}
				else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--mountcfg", "-mountcfg"}.Contains(args[i]))
				{
					string fixed_path = i < args.Length - 1 ? PathCombine(args[i + 1]) : null;
					if(i == args.Length - 1)
					{
						Log("Missing path argument for \'"+args[i]+"\' parameter", LogType.Warning);
						continue;
					}
					else if(fixed_path == null || !IsFullPath(fixed_path))
					{
						Log("Invalid path specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}
					/*else if(!Path.GetExtension(fixed_path).EqualsCI(".cfg"))
					{
						Log("Improper file extension specified for \'"+args[i]+"\' parameter", LogType.Warning);
					}*/
					else
					{
						mount_cfg_path = fixed_path;
					}
					i++;
				}
				else if(args[i] == "-l" || new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--log", "-log"}.Contains(args[i]))
				{
					cmd_options[0] = args[i];
					if(!cmd)checkBox2.Checked = true; //i.e. log = true
					else log = true;
				}
				else if(args[i] == "-n" || new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--notify", "-notify"}.Contains(args[i]))
				{
					cmd_options[1] = args[i];
					notify = true;
				}
				else if(args[i] == "-u" || new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--use-native-tools", "-use-native-tools"}.Contains(args[i]))
				{
					cmd_options[2] = args[i];
					use_native_tools = true;
				}
				else if(args[i] == "-b" || new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--no-backup", "-no-backup"}.Contains(args[i]))
				{
					cmd_options[3] = args[i];
					do_backup = false;
				}
				else if(args[i] == "-p" || new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--no-pack", "-no-pack"}.Contains(args[i])) //!!this option will be needed in the future; currently added to be able to run the program via GUI with the "Pack" checkbox unchecked
				{
					cmd_options[4] = args[i];
					if(!cmd)checkBox1.Checked = false; //i.e. pack = false
					else pack = false;
				}
				else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"--addcfg", "-addcfg"}.Contains(args[i])) //this condition block is used to skip the path of this parameter (as it's handled in the previous loop) and then check if the argument is unknown
				{
					if(i == args.Length - 1)continue;
					i++;
				}
				else
				{
					Log("Unknown argument \'"+args[i]+"\'", LogType.Warning);
				}
			}

			if(cmd)
			{
				if(input_vmf == "")
				{
					CmdCloseReason("VMF file is not specified");
					return;
				}
				if(input_bsp == "")
				{
					pack = false;
				}
				if(game == -1)
				{
					CmdCloseReason("Game is not specified");
					return;
				}
			}
			else
			{
				if(game == -1)game = 0; //default game is set here
				notify = true;
				toolTip1.SetToolTip(button3, "Game: "+games_info[game].game_folder);
			}
			
			if(steam_path == "" && game_directory != null)
			{
				string[] split_directory = PathCombine(game_directory)?.Split('/');
				if(split_directory != null)
				{
					int steamapps_folder_index = Array.FindIndex(split_directory, item => item.EqualsCI("steamapps"));
					if(steamapps_folder_index != -1)
					{
						string new_steam_path = string.Join("/", split_directory.Take(steamapps_folder_index));
						if(IsSteamPath(new_steam_path))
						{
							steam_path = new_steam_path;
						}
					}
				}
			}

			extra_files = games_info[game].available_extra_files.ToArray();
			settings = games_info[game].available_settings.ToArray();
			{
				string OR = @"|";
				string path_start = @"(?<![\./\\])\b";
				string path_end = @"\b(?![\./\\])";
				string path_character = @"[^"+string.Join("", Path.GetInvalidFileNameChars().Except(new HashSet<char>{'/', '\\'/*, '\0'*/}).Concat(new char[]{' '}).Select(item => Regex.Escape(item.ToString())))+@"]";
				script_master_pattern =
					@"(?<path>"+path_start+@"models(?:/|\\)"+path_character+@"+?(?<extension>\.mdl))"+path_end + //models
					OR + @"(?<path>"+path_start+path_character+@"+?(?<extension>\.nut))"+path_end + //.nut scripts
					OR + @"(?<path>"+path_start+path_character+@"+?(?<extension>\.cfg))"+path_end + //.cfg scripts
					OR + path_start+@"["+string.Join("", Constants.sound_characters.Select(item => Regex.Escape(item.ToString())))+@"]*(?<path>"+path_character+@"+?(?<extension>\.(?:"+string.Join(@"|", Constants.sound_extensions)+@")))"+path_end + //sounds
					(extra_files[(int)ExtraFiles.BotsBehavior] ? OR + @"(?<path>"+path_start+@"scripts(?:/|\\)"+path_character+@"+?(?<extension>\.kv3))"+path_end : ""); //kv3 files
			}

			if(!cmd) //calls EnumerateFiles in advance so that it runs faster later; I hope that's exactly how it works
			{
				DictionaryList<string, string> library_paths = GetBlockData(new ContentInfo{file_path = PathCombine(steam_path, "steamapps", "libraryfolders.vdf")}, "libraryfolders", "path", data_search_option: SearchOption.AllDirectories, parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues;
				if(library_paths != null)
				{
					for(int i = 0;i < library_paths[0].Value.Count;i++)
					{
						string current_path = PathCombine(library_paths[0].Value[i]);
						if(current_path != null)
						{
							Directory.EnumerateFiles(PathCombine(current_path, "steamapps"), "appmanifest_*.acf", SearchOption.TopDirectoryOnly);
						}
					}
				}
			}

			if(cmd)ButtonPackData();
		}

		protected override void SetVisibleCore(bool value)
		{
			if(!IsHandleCreated && value)
			{
				value = false;
				StartPosition = FormStartPosition.CenterScreen;
				try //try-catch is used to prevent a crash after calling CmdCloseReason
				{
					CreateHandle();
				}
				catch
				{
					Application.Exit();
				}
			}
			base.SetVisibleCore(value);
		}

		//[DllImport("user32.dll")]
		//private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		internal static extern bool PostMessage(IntPtr hWnd, Int32 msg, Int32 wParam, Int32 lParam);
		static Int32 WM_SYSCOMMAND = 0x0112;
		static Int32 SC_RESTORE = 0xF120;

		[DllImport("kernel32.dll")]
		private static extern bool AttachConsole(int dwProcessId);
		private const int ATTACH_PARENT_PROCESS = -1;

		//main
		Thread pack_thread = null;
		string input_bsp = "";
		string input_vmf = "";
		string vmf_name = "";
		public static string cache_path = PathCombine(Environment.CurrentDirectory, "cache");
		public static string cfgs_path = PathCombine(Environment.CurrentDirectory, "game_cfgs");
		public static string extracted_assets_path = PathCombine(cache_path, "extracted_assets");
		bool packing = false;
		public static readonly List<GameInfo> games_info_default = new List<GameInfo>
		{
			new GameInfo
			{
				game_folder = "Counter-Strike Global Offensive",
				game_root_folder = "csgo",
				vpk_paths = new List<string>
				{
					"csgo/pak01_dir.vpk",
					"platform/platform_pak01_dir.vpk" //some tool textures are stored there
				},
				available_extra_files = new bool[]{true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true},
				available_settings = new bool[]{true, true, false},
				assets_search_settings = new bool[]{false, true, true, true},
				assets_search_order = new List<AssetsSearchOrderOption>
				{
					AssetsSearchOrderOption.OwnVPKs,
					AssetsSearchOrderOption.OwnGameFolder
				}
			},
			new GameInfo
			{
				game_folder = "GarrysMod",
				game_root_folder = "garrysmod",
				vpk_paths = new List<string>
				{
					"garrysmod/garrysmod_dir.vpk",
					"garrysmod/fallbacks_dir.vpk",
					"sourceengine/hl2_misc_dir.vpk",
					"sourceengine/hl2_textures_dir.vpk",
					"sourceengine/hl2_sound_misc_dir.vpk",
					"sourceengine/hl2_sound_vo_english_dir.vpk"
				},
				available_extra_files = new bool[]{true, true, false, true, false, false, false, false, false, true, true, false, false, false, false, false, false, false, false},
				available_settings = new bool[]{true, false, true},
				assets_search_settings = new bool[]{true, true, true, true},
				assets_search_order = new List<AssetsSearchOrderOption>
				{
					AssetsSearchOrderOption.OwnGameFolder,
					AssetsSearchOrderOption.OwnVPKs,
					AssetsSearchOrderOption.MountedVPKs,
					AssetsSearchOrderOption.MountedFolders
				}
			},
			new GameInfo
			{
				game_folder = "Portal 2",
				game_root_folder = "portal2",
				vpk_paths = new List<string>
				{
					"portal2/pak01_dir.vpk",
					"portal2_dlc1/pak01_dir.vpk",
					"portal2_dlc2/pak01_dir.vpk"
				},
				available_extra_files = new bool[]{false, false, false, true, false, false, false, false, false, true, true, false, false, false, false, false, false, false, false},
				available_settings = new bool[]{true, true, true},
				assets_search_settings = new bool[]{true, false, false, true},
				assets_search_order = new List<AssetsSearchOrderOption>
				{
					AssetsSearchOrderOption.MountedVPKs,
					AssetsSearchOrderOption.MountedFolders
				}
			}
		};
		public List<GameInfo> games_info = games_info_default.Select(item => new GameInfo{game_folder = item.game_folder, game_root_folder = item.game_root_folder, vpk_paths = item.vpk_paths.ToList(), available_extra_files = item.available_extra_files.ToArray(), available_settings = item.available_settings.ToArray(), assets_search_settings = item.assets_search_settings.ToArray(), assets_search_order = item.assets_search_order.ToList()}).ToList();
		//settings
		bool log = false;
		bool pack = true;
		bool notify = false;
		bool use_native_tools = false;
		bool do_backup = true;
		bool cmd = false;
		public string steam_path = "";
		public int game = -1;
		public bool[] extra_files = {};
		public bool[] settings = {};
		//other
		HashSet<string> vmfs_found = null;
		Queue<string> vmfs_to_pack = null;
		HashList<string> custom_files = null;
		List<string> custom_files_paths = null;
		Dictionary<string, HashSet<string>> game_vpk_files = null;
		List<string> search_paths = null;
		Dictionary<int, Dictionary<string, VPKFile>> search_vpks = null;
		Dictionary<string, int> extracted_files = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); //stores relative paths of extracted files and the index of vpk they come from in search_paths array
		string library_path = null;
		string gameinfo_path = null;
		string mount_cfg_path = null;
		string notification_text;
		string[] cmd_options = new string[5];
		List<KeyValuePair<string, LogType>> deferred_log = new List<KeyValuePair<string, LogType>>();
		int warnings_amount = 0;
		//regex patterns
		string VMT_texture_keys_pattern = @"^(?:.*\?)?("+string.Join(@"|", Constants.VMT_texture_keys.Select(item => (item.Key[0].IsRegexEscapable() ? @"\" : "")+item.Key))+@")$";
		string VMT_material_keys_pattern = @"^("+string.Join(@"|", Constants.VMT_material_keys.Select(item => (item.Key[0].IsRegexEscapable() ? @"\" : "")+item.Key))+@")$";
		string VMF_material_keys_pattern = @"^("+string.Join(@"|", Constants.VMF_material_keys.Select(item => item.Key))+@")$";
		string VMF_model_keys_pattern = @"^("+string.Join(@"|", Constants.VMF_model_keys.Select(item => item.Key))+@")$";
		string script_master_pattern;
		//misc
		string developer_link = "https://github.com/drinklc";
		UpdateCfgOption update_cfgs = UpdateCfgOption.Ask;
		NotifyIcon notifyIcon;


		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start(developer_link);
		}

		/*private void Form1_Load(object sender, EventArgs e)
		{
			VoidLoad();
		}*/

		private void VoidLoad()
		{
			if(!cmd && !Debugger.IsAttached)
			{
				string repository_id = "858869233";
				string user_agent = "AutoBSPpackingTool";
				ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; //HttpClient is not available in .NET Framework 4.0
				using TimedWebClient web_client = new TimedWebClient(){Timeout = 3000};
				web_client.Headers.Add("User-Agent", user_agent);
				string response = null;
				try
				{
					response = web_client.DownloadString("https://api.github.com/repositories/"+repository_id+"/releases/latest");
				}
				catch{}
				if(response != null)
				{
					try
					{
						JavaScriptSerializer json_serializer = new JavaScriptSerializer();
						Dictionary<string, object> latest_release_info = json_serializer.Deserialize<Dictionary<string, object>>(response);
						string release_tag_name = (string)latest_release_info?["tag_name"];
						if(release_tag_name == null)throw new Exception();
						Version latest_version = new Version(release_tag_name);
						Version current_version = GetProgramVersion();
						if(latest_version > current_version)
						{
							if(MessageBox.Show("A newer version of the program is available.\nWould you like to download it?\n\nCurrent version: "+current_version.ToString()+"\nLatest version: "+latest_version.ToString(), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
							{
								foreach(object asset in latest_release_info["assets"] as ArrayList)
								{
									if(((string)((Dictionary<string, object>)asset)["name"]).EqualsCI("AutoBSPpackingTool.exe"))
									{
										Process.Start((string)((Dictionary<string, object>)asset)["browser_download_url"]);
										break;
									}
								}
								Close();
								return;
							}
						}
						else if(latest_version < current_version)
						{
							if(MessageBox.Show("You are using a version of the program that is newer than the official latest release. This may be a debug or test version and could contain unfinished features or potential bugs.\nPlease do not distribute or share this version.\nIf you select 'Yes', you agree to use the program under these terms.\n\nCurrent version: "+current_version.ToString()+"\nLatest version: "+latest_version.ToString(), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
							{
								Close();
								return;
							}
						}
						web_client.Headers.Add("User-Agent", user_agent);
						Dictionary<string, object> repository_info = json_serializer.Deserialize<Dictionary<string, object>>(web_client.DownloadString("https://api.github.com/repositories/"+repository_id));
						developer_link = (string)((Dictionary<string, object>)repository_info?["owner"])?["html_url"] ?? developer_link;
					}
					catch{}
				}
			}
			if(Directory.Exists(cfgs_path))
			{
				foreach(string game_cfg in Directory.EnumerateFiles(cfgs_path, "*.txt", SearchOption.TopDirectoryOnly))
				{
					AddGameCfg(game_cfg);
				}
			}
		}

		private void Form1_Shown(object sender, EventArgs e)
		{
			VoidShown();
		}

		private void VoidShown()
		{
			if(steam_path == "")
			{
				if(MessageBox.Show("Could not find steam directory. You can specify it in the settings.", Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
				{
					OpenSettings();
				}
			}
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(pack_thread != null)
			{
				pack_thread.Abort();
			}
			if(packing)
			{
				try //try-catch is used to quickly ensure that the file is not being accessed by other processes and there's permission to access it
				{
					File.Delete(PathCombine(cache_path, vmf_name+"_bspzip_files_list.txt"));
				}
				catch{}
				Log("Packing was forced to stop"+(e.CloseReason == CloseReason.UserClosing ? " by user" : " (program close reason: "+e.CloseReason+")"), LogType.Error, true);
			}
			try //try-catch is used instead of checking whether the directory exists and has any subfolders/files
			{
				Directory.Delete(cache_path);
			}
			catch{}
		}

		private void HandleUnhandledException(Exception exception)
		{
			if(packing)
			{
				Log(exception.ToString(), LogType.Fatal, true);
				Application.Exit();
			}
			else if(!Debugger.IsAttached)
			{
				if(new ThreadExceptionDialog(exception).ShowDialog() == DialogResult.Abort)
				{
					Application.Exit();
				}
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			using(OpenFileDialog file_dialog = new OpenFileDialog{FileName = "VMF file", Filter = "VMF Files (*.VMF)|*.VMF", InitialDirectory = input_vmf != "" ? Path.GetDirectoryName(input_vmf) : ""})
			{
				if(file_dialog.ShowDialog() == DialogResult.OK)
				{
					ChangeVMF(file_dialog.FileName);
				}
			}
		}

		private void ChangeVMF(string path)
		{
			path = PathCombine(path);
			input_vmf = path;
			vmf_name = Path.GetFileNameWithoutExtension(path);
			label2.Text = ShortenFileName(Path.GetFileName(path), 30);
			int panel_x = Round((ClientSize.Width - (button1.Size.Width + 4 + label2.Size.Width)) / 2f);
			button1.Location = new Point(panel_x, button1.Location.Y);
			label2.Location = new Point(panel_x + button1.Size.Width + 4, label2.Location.Y);
			CheckMainButtonEnabled();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			using(OpenFileDialog file_dialog = new OpenFileDialog{FileName = "BSP file", Filter = "BSP Files (*.BSP)|*.BSP", InitialDirectory = input_bsp != "" ? Path.GetDirectoryName(input_bsp) : ""})
			{
				if(file_dialog.ShowDialog() == DialogResult.OK)
				{
					ChangeBSP(file_dialog.FileName);
				}
			}
		}

		private void ChangeBSP(string path)
		{
			path = PathCombine(path);
			input_bsp = path;
			label4.Text = ShortenFileName(Path.GetFileName(path), 30);
			int panel_x = Round((ClientSize.Width - (button2.Size.Width + 4 + label4.Size.Width)) / 2f);
			button2.Location = new Point(panel_x, button2.Location.Y);
			label4.Location = new Point(panel_x + button2.Size.Width + 4, label4.Location.Y);
			CheckMainButtonEnabled();
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			pack = checkBox1.Checked;
			button3.Text = pack ? "Pack files" : "List files";
			label3.Enabled =
			label4.Enabled =
			button2.Enabled = pack;
			CheckMainButtonEnabled();
		}

		private void checkBox2_CheckedChanged(object sender, EventArgs e)
		{
			log = checkBox2.Checked;
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			OpenSettings();
		}

		void Notify(string title, string text, int time = 1000, ToolTipIcon tool_tip_icon = ToolTipIcon.Info)
		{
			notifyIcon.ShowBalloonTip(time, title, text, tool_tip_icon);
		}

		void CheckMainButtonEnabled()
		{
			button3.Enabled = input_vmf != "" && (!pack || input_bsp != "");
		}

		void OpenSettings()
		{
			Form2 settings_form = new Form2(){main_form = this};
			settings_form.ShowDialog();
		}

		void DetectSteamDirectory()
		{
			KeyValuePair<string, string>[] possible_paths =
			{
				new (@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath"),
				new (@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath"),
				new (@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath")
			};
			for(int i = 0;i < possible_paths.Length;i++)
			{
				string current_possible_path = GetRegistryValue(possible_paths[i].Key, possible_paths[i].Value);
				if(current_possible_path != null && IsSteamPath(current_possible_path))
				{
					steam_path = PathCombine(current_possible_path);
					return;
				}
			}
		}

		bool IsSteamPath(string path)
		{
			return File.Exists(PathCombine(path, "steam.exe")) && Directory.Exists(PathCombine(path, "bin"));
		}

		public FindLibraryDirectoryResult FindLibraryDirectory(string game_folder, string game_root_folder, out string library_path)
		{
			library_path = null;
			string libraryfolders_path = PathCombine(steam_path, "steamapps", "libraryfolders.vdf");

			if(!File.Exists(libraryfolders_path))
			{
				if(Directory.Exists(PathCombine(steam_path, "steamapps/common", game_folder, game_root_folder)))
				{
					library_path = PathCombine(steam_path);
					return FindLibraryDirectoryResult.Found;
				}
				return FindLibraryDirectoryResult.LibraryFoldersVDFNotFound;
			}

			DictionaryList<string, string> library_paths = GetBlockData(new ContentInfo{file_path = libraryfolders_path}, "libraryfolders", "path", data_search_option: SearchOption.AllDirectories, parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues;
			if(library_paths != null)
			{
				for(int i = 0;i < library_paths[0].Value.Count;i++)
				{
					string current_path = PathCombine(library_paths[0].Value[i]);
					if(current_path != null)
					{
						foreach(string app_manifest in Directory.EnumerateFiles(PathCombine(current_path, "steamapps"), "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
						{
							DictionaryList<string, string> app_manifest_keyvalues = GetBlockData(new ContentInfo{file_path = app_manifest}, "AppState", "installdir", parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues;
							if(app_manifest_keyvalues?.Count > 0 && app_manifest_keyvalues[0].Value[0].EqualsCI(game_folder))
							{
								library_path = current_path;
								return FindLibraryDirectoryResult.Found;
							}
						}
					}
				}
			}
			return FindLibraryDirectoryResult.AppManifestNotFound;
		}

		void Log(string text, LogType log_type = LogType.Information, bool print_timestamp = false, string notification_text_override = null)
		{
			if(log_type == LogType.Error)notification_text = notification_text_override ?? text;
			text = (print_timestamp ? "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss.fff")+"] " : "") + log_type.GetTextField() + (log_type != LogType.None ? " " : "") + text;
			if(log)
			{
				if(packing)
				{
					string log_path = PathCombine(cache_path, vmf_name+".log");
					using(StreamWriter stream_writer = new StreamWriter(log_path, true))
					{
						stream_writer.WriteLine(text);
					}
					if(log_type == LogType.Warning)warnings_amount++;
				}
				else
				{
					deferred_log.Add(new (text, log_type));
				}
			}
			if(cmd && (!log || packing))Console.WriteLine(text);
		}

		void LogFilesInfo(int all_files_amount, int custom_files_amount, string info)
		{
			Log(all_files_amount + " " + string.Format(info, all_files_amount != 1 ? "s" : "") + " w" + (all_files_amount != 1 ? "ere" : "as") + " detected" + (all_files_amount > 0 ? "; " + custom_files_amount + " " + (custom_files_amount != 1 ? "are" : "is") + " custom" : ""));
		}

		void ConsoleText(string text)
		{
			if(cmd)return;
			Invoke(new Action(() => label5.Text = text));
		}

		void CmdCloseReason(string message)
		{
			message = LogType.Error.GetTextField() + " " + message;
			if(log)
			{
				if(!Directory.Exists(cache_path))Directory.CreateDirectory(cache_path);
				File.WriteAllText(PathCombine(cache_path, "cmd_exit_reason.log"), message);
			}
			if(cmd)Console.WriteLine(message);
			Close();
		}

		void SetProgress(int value)
		{
			if(cmd)return;
			Invoke(new Action(() =>
			{
				progressBar1.Value = value;
				if(Environment.OSVersion.Version >= new Version(6, 1))TaskbarProgress.SetValue(Handle, value, 100); //starting from Windows 7
			}));
		}

		void AddGameCfg(string path)
		{
			DictionaryList<string, string> cfg_keyvalues = GetBlockData(new ContentInfo{file_path = path}, @"(?:game_config|data)", parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues;
			if(cfg_keyvalues == null)return;
			if(!cfg_keyvalues.TryGetValue("game_folder", out string game_folder_cfg))return;
			if(!cfg_keyvalues.TryGetValue("root_folder", out string root_folder_cfg))return;
			if(game_folder_cfg.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 || root_folder_cfg.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)return;
			string vpks_cfg = cfg_keyvalues.ContainsKey("vpks") ? cfg_keyvalues["vpks"][0] : "";
			if(vpks_cfg.IndexOfAny(Path.GetInvalidFileNameChars().Except(new HashSet<char>{'/', '\\', ':', '|'}).ToArray()) != -1)return;
			Version config_version;
			try
			{
				config_version = new Version(cfg_keyvalues["config_version"][0]);
			}
			catch
			{
				config_version = new Version();
			}
			bool[] available_extra_files_cfg = cfg_keyvalues.ContainsKey("available_extra_files") ? cfg_keyvalues["available_extra_files"][0].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries).Select(item => item == "1").ToArray() : new bool[]{};
			bool[] available_settings_cfg = cfg_keyvalues.ContainsKey("available_settings") ? cfg_keyvalues["available_settings"][0].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries).Select(item => item == "1").ToArray() : cfg_keyvalues.ContainsKey("available_extra_settings") ? cfg_keyvalues["available_extra_settings"][0].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries).Select(item => item == "1").ToArray() : new bool[]{};
			bool[] assets_search_settings_cfg = cfg_keyvalues.ContainsKey("assets_search_settings") ? cfg_keyvalues["assets_search_settings"][0].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries).Select(item => item == "1").ToArray() : new bool[]{};
			string[] assets_search_order_cfg_raw = cfg_keyvalues.ContainsKey("assets_search_order") ? cfg_keyvalues["assets_search_order"][0].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries) : null;
			long config_priority_cfg = cfg_keyvalues.ContainsKey("config_priority") && long.TryParse(cfg_keyvalues["config_priority"][0], out config_priority_cfg) ? config_priority_cfg : GetUnixTimeMilliseconds();

			if(config_version < new Version(18, 0, 0))
            {
				if(available_extra_files_cfg.Length == 20)
                {
					Array.Copy(available_extra_files_cfg, 13, available_extra_files_cfg, 12, 7);
                }
            }

			//this allows to add new settings in future updates, as well as support newer configs in older versions of the program
			if(available_extra_files_cfg.Length != games_info_default[0].available_extra_files.Length)
			{
				int prev_length = available_extra_files_cfg.Length;
				Array.Resize(ref available_extra_files_cfg, games_info_default[0].available_extra_files.Length);
				for(int i = prev_length;i < available_extra_files_cfg.Length;i++)
				{
					available_extra_files_cfg[i] = true;
				}
			}
			if(available_settings_cfg.Length != games_info_default[0].available_settings.Length)
			{
				int prev_length = available_settings_cfg.Length;
				Array.Resize(ref available_settings_cfg, games_info_default[0].available_settings.Length);
				for(int i = prev_length;i < available_settings_cfg.Length;i++)
				{
					available_settings_cfg[i] = true;
				}
			}
			if(assets_search_settings_cfg.Length != games_info_default[0].assets_search_settings.Length)
			{
				int prev_length = assets_search_settings_cfg.Length;
				Array.Resize(ref assets_search_settings_cfg, games_info_default[0].assets_search_settings.Length);
				for(int i = prev_length;i < assets_search_settings_cfg.Length;i++)
				{
					assets_search_settings_cfg[i] = true;
				}
			}
			if(!available_settings_cfg[(int)Settings.DetectCustomSearchPaths] && !assets_search_settings_cfg[(int)AssetsSearchSettings.AddExtraOwnVPKs] && !assets_search_settings_cfg[(int)AssetsSearchSettings.AddExtraOwnGameFolder])available_settings_cfg[(int)Settings.DetectCustomSearchPaths] = true;
			List<AssetsSearchOrderOption> assets_search_order_cfg;
			List<AssetsSearchOrderOption> expected_assets_search_order_options = new List<AssetsSearchOrderOption>(Enum.GetValues(typeof(AssetsSearchOrderOption)).Length - 1);
			if(assets_search_settings_cfg[(int)AssetsSearchSettings.AddExtraOwnVPKs])expected_assets_search_order_options.Add(AssetsSearchOrderOption.OwnVPKs);
			if(assets_search_settings_cfg[(int)AssetsSearchSettings.AddExtraOwnGameFolder])expected_assets_search_order_options.Add(AssetsSearchOrderOption.OwnGameFolder);
			if(available_settings_cfg[(int)Settings.DetectCustomSearchPaths])
			{
				if(assets_search_settings_cfg[(int)AssetsSearchSettings.SeparateMountedFoldersAndVPKs])
				{
					expected_assets_search_order_options.Add(AssetsSearchOrderOption.MountedVPKs);
					expected_assets_search_order_options.Add(AssetsSearchOrderOption.MountedFolders);
				}
				else
				{
					expected_assets_search_order_options.Add(AssetsSearchOrderOption.MountedFiles);
				}
			}
			if(assets_search_order_cfg_raw?.All(item => int.TryParse(item, out int item_int) && Enum.IsDefined(typeof(AssetsSearchOrderOption), item_int)) != true || !IsPermutation(assets_search_order_cfg = assets_search_order_cfg_raw.Select(item => (AssetsSearchOrderOption)int.Parse(item)).ToList(), expected_assets_search_order_options))
			{
				assets_search_order_cfg = expected_assets_search_order_options;
			}

			if(config_version < Constants.config_format_changed_since)
			{
				if(cmd)
				{
					//make quick edits here if needed
				}
				else
				{
					DialogResult message_box = DialogResult.None;
					if(update_cfgs == UpdateCfgOption.Ask)
					{
						message_box = MessageBox.Show("Your custom game configurations are in an outdated format.\nWould you like to update and overwrite them?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
					}
					if(update_cfgs == UpdateCfgOption.Update || message_box == DialogResult.Yes)
					{
						update_cfgs = UpdateCfgOption.Update;
						//make edits here if needed
						File.WriteAllText(path, GetGameCfg(game_folder_override: game_folder_cfg, root_folder_override: root_folder_cfg, vpks_override: vpks_cfg, available_extra_files_override: available_extra_files_cfg, available_settings_override: available_settings_cfg, assets_search_settings_override: assets_search_settings_cfg, assets_search_order_override: assets_search_order_cfg, config_priority_override: config_priority_cfg.ToString()));
					}
					else if(update_cfgs == UpdateCfgOption.Skip || message_box == DialogResult.No)
					{
						update_cfgs = UpdateCfgOption.Skip;
						return;
					}
				}
			}

			int default_index = -1;
			for(int k = 0;k < games_info_default.Count;k++)
			{
				if(games_info_default[k].game_folder.EqualsCI(game_folder_cfg))
				{
					if(games_info_default[k].game_root_folder.EqualsCI(root_folder_cfg))
					{
						default_index = k;
						break;
					}
					else
					{
						return;
					}
				}
			}

			if(default_index == -1)
			{
				if(games_info.Any(item => item.game_folder.EqualsCI(game_folder_cfg)))return; //this doesn't allow adding two custom cfgs with the same game folder
				games_info.Add(new GameInfo
				{
					game_folder = game_folder_cfg,
					game_root_folder = root_folder_cfg,
					vpk_paths = vpks_cfg.Split(new char[]{'|'}, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Replace('\\', '/')).ToList(),
					available_extra_files = available_extra_files_cfg,
					available_settings = available_settings_cfg,
					assets_search_settings = assets_search_settings_cfg,
					assets_search_order = assets_search_order_cfg,
					config_priority = config_priority_cfg
				});
			}
			else
			{
				GameInfo new_game_info = games_info[default_index];
				new_game_info.vpk_paths = vpks_cfg.Split(new char[]{'|'}, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Replace('\\', '/')).ToList();
				new_game_info.available_extra_files = available_extra_files_cfg;
				new_game_info.available_settings = available_settings_cfg;
				new_game_info.assets_search_settings = assets_search_settings_cfg;
				new_game_info.assets_search_order = assets_search_order_cfg;
				games_info[default_index] = new_game_info;
			}
		}

		public string GetGameCfg(int index = -1, ConfigOption config_option = ConfigOption.Actual, string game_folder_override = default, string root_folder_override = default, string vpks_override = default, bool[] available_extra_files_override = default, bool[] available_settings_override = default, bool[] assets_search_settings_override = default, List<AssetsSearchOrderOption> assets_search_order_override = default, string config_priority_override = default) //this function doesn't use json since I wanted to keep the cfgs as simple as possible
		{
			List<KeyValuePair<string, string>> save_keyvalues = new List<KeyValuePair<string, string>>()
			{
				new ("game_folder", game_folder_override == default ? (config_option == ConfigOption.Actual ? games_info : games_info_default)[index].game_folder : game_folder_override),
				new ("root_folder", root_folder_override == default ? (config_option == ConfigOption.Actual ? games_info : games_info_default)[index].game_root_folder : root_folder_override),
				new ("vpks", vpks_override == default ? string.Join("|", (config_option == ConfigOption.Actual ? games_info : games_info_default)[index].vpk_paths) : vpks_override),
				new ("available_extra_files", string.Join(" ", (available_extra_files_override == default ? (config_option == ConfigOption.Actual ? games_info : games_info_default)[index].available_extra_files : available_extra_files_override).Select(item => item ? "1" : "0"))),
				new ("available_settings", string.Join(" ", (available_settings_override == default ? (config_option == ConfigOption.Actual ? games_info : games_info_default)[index].available_settings : available_settings_override).Select(item => item ? "1" : "0"))),
				new ("assets_search_settings", string.Join(" ", (assets_search_settings_override == default ? (config_option == ConfigOption.Actual ? games_info : games_info_default)[index].assets_search_settings : assets_search_settings_override).Select(item => item ? "1" : "0"))),
				new ("assets_search_order", string.Join(" ", (assets_search_order_override == default ? (config_option == ConfigOption.Actual ? games_info : games_info_default)[index].assets_search_order : assets_search_order_override).Select(item => (int)item))),
				new ("config_version", Constants.config_format_changed_since.ToString())
			};
			if(config_option == ConfigOption.Actual)
			{
				save_keyvalues.Add(new ("config_priority", config_priority_override == default ? games_info[index].config_priority.ToString() : config_priority_override));
			}
			return string.Join("\r\n", new List<string>{"game_config", "{"}.Concat(save_keyvalues.Select(item => "\t\""+item.Key+"\" \""+item.Value+"\"")).Concat(new List<string>{"}"}));
		}

		void DetectSearchPaths()
		{
			string current_gameinfo_path = gameinfo_path ?? PathCombine(library_path, "steamapps/common", games_info[game].game_folder, games_info[game].game_root_folder, "gameinfo.txt");
			string current_mount_cfg_path = mount_cfg_path ?? PathCombine(library_path, "steamapps/common", games_info[game].game_folder, games_info[game].game_root_folder, "cfg", "mount.cfg");
			if(!File.Exists(current_gameinfo_path))
			{
				Log(gameinfo_path == null ? "Could not find gameinfo.txt file" : "The specified gameinfo.txt file doesn't exist", LogType.Warning);
				gameinfo_path = null;
			}
			/*if(!Path.GetFileName(current_gameinfo_path).EqualsCI("gameinfo.txt"))
			{
				Log("Specified gameinfo file is not named \"gameinfo.txt\"", LogType.Warning);
			}*/
			bool use_mount_cfg = games_info[game].game_folder.EqualsCI("GarrysMod");
			if(use_mount_cfg)
			{
				if(File.Exists(current_mount_cfg_path))
				{
					Log(".cfg (mount configuration) file was detected");
				}
				else if(mount_cfg_path != null)
				{
					Log("Specified mount configuration file doesn't exist", LogType.Warning);
				}
			}

			search_paths = new List<string>();

			KeyValuePair<string, string>[] variables =
			{
				new ("all_source_engine_paths", games_info[game].game_folder),
				new ("gameinfo_path", PathCombine(games_info[game].game_folder, games_info[game].game_root_folder))
			};

			string ReplaceVariables(string path)
			{
				for(int i = 0;i < variables.Length;i++)
				{
					if(path.StartsWithCI("|"+variables[i].Key+"|"))
					{
						return PathCombine(library_path, "steamapps/common", variables[i].Value, path.Remove(0, variables[i].Key.Length + 2));
					}
				}
				return PathCombine(path);
			}

			void AddVPKs(string path)
			{
				if(!games_info[game].assets_search_settings[(int)AssetsSearchSettings.ScanForVPKs])return;
				search_paths.AddRange(Directory.EnumerateFiles(path, "*_dir.vpk", SearchOption.TopDirectoryOnly).Select(item => PathCombine(item)));
			}

			void AddPaths(DictionaryList<string, string> paths_keys, bool use_variables = true, bool only_directories = false)
			{
				foreach(KeyValuePair<string, string> current_keyvalue in paths_keys)
				{
					string path = use_variables ? ReplaceVariables(current_keyvalue.Value) : PathCombine(current_keyvalue.Value);
					if(path != null)
					{
						bool add_subdirectories = false;
						if(path.EndsWith("/*"))
						{
							add_subdirectories = true;
							path = path.Remove(path.Length - 1, 1);
						}
						try //handles the "invalid path" exception
						{
							string full_path = PathCombine(Path.GetFullPath(IsFullPath(path) ? path : PathCombine(library_path, "steamapps/common", games_info[game].game_folder, current_keyvalue.Key.EqualsCI("vpk") ? "vpks" : "", path)));
							if(!only_directories && current_keyvalue.Key.EqualsCI("vpk"))
							{
								if(!add_subdirectories && full_path[full_path.Length - 1] != '/' && new HashSet<string>(StringComparer.OrdinalIgnoreCase){"", ".vpk"}.Contains(Path.GetExtension(full_path)))
								{
									string vpk_name = Path.ChangeExtension(full_path, null);
									if(!vpk_name.EndsWithCI("_dir"))vpk_name += "_dir";
									vpk_name += ".vpk";
									if(File.Exists(vpk_name))
									{
										search_paths.Add(vpk_name);
									}
								}
							}
							else if(Directory.Exists(full_path))
							{
								full_path = full_path.TrimEnd('/')+'/';
								if(!add_subdirectories)
								{
									/*if(!steam_language.EqualsCI("english"))
									{
										string language_path = full_path+"_"+steam_language;
										if(Directory.Exists(language_path))
										{
											search_paths.Add(language_path);
											AddVPKs(language_path);
										}
									}*/
									search_paths.Add(full_path);
									AddVPKs(full_path);
								}
								else
								{
									AddVPKs(full_path);
									foreach(string current_subfolder in Directory.EnumerateDirectories(full_path))
									{
										search_paths.Add(PathCombine(current_subfolder));
										AddVPKs(current_subfolder);
									}
								}
							}
							else if(!only_directories && Path.GetExtension(full_path).EqualsCI(".vpk"))
							{
								string vpk_name = full_path.EndsWithCI("_dir.vpk") ? full_path : Path.ChangeExtension(full_path, null) + "_dir.vpk";
								if(File.Exists(vpk_name))
								{
									search_paths.Add(vpk_name);
								}
							}
						}
						catch{}
					}
				}
			}

			if(!use_mount_cfg && File.Exists(current_gameinfo_path))
			{
				DictionaryList<string, string> game_keys_gameinfo = GetBlockData(new ContentInfo{file_path = current_gameinfo_path}, "SearchPaths", @"(?:[^\+]+\+)*(game|vpk)(?:\+[^\+]+)*", replace_keys_with_this_matched_group: 1)?[0]?.keyvalues;
				if(game_keys_gameinfo != null)
				{
					AddPaths(game_keys_gameinfo);
				}
			}

			if(use_mount_cfg && File.Exists(current_mount_cfg_path))
			{
				DictionaryList<string, string> game_keys_mountcfg = GetBlockData(new ContentInfo{file_path = current_mount_cfg_path}, "mountcfg", parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues;
				if(game_keys_mountcfg != null)
				{
					AddPaths(game_keys_mountcfg, false, true);
				}
			}

			search_paths = search_paths.Distinct(StringComparer.OrdinalIgnoreCase)/*.Except(games_info[game].vpk_paths.Select(item => IsFullPath(item) ? PathCombine(item) : PathCombine(library_path, "steamapps/common", games_info[game].game_folder, item)), StringComparer.OrdinalIgnoreCase)*/.ToList();
		}

		void ReadSearchVpks()
		{
			int search_vpks_amount = search_paths.Count(item => Path.GetExtension(item).EqualsCI(".vpk") && !game_vpk_files.ContainsKey(item));
			search_vpks = new Dictionary<int, Dictionary<string, VPKFile>>(search_vpks_amount);
			int vpk_number = 0;
			for(int i = 0;i < search_paths.Count;i++)
			{
				if(!Path.GetExtension(search_paths[i]).EqualsCI(".vpk") || game_vpk_files.ContainsKey(search_paths[i]))continue;
				ConsoleText("Reading ("+(++vpk_number)+"/"+(search_vpks_amount + game_vpk_files.Count)+"): "+ShortenFileName(Path.GetFileName(search_paths[i]), 30));
				/*if(!use_native_tools) //this code is commented out since vpk.exe doesn't seem to be able to extract individual files, hence VPKFile structs in search_vpks are needed, which cannot be achieved with vpk.exe
				{
					//add ", try running without the '"+cmd_options[2]+"' option" below if uncommenting this block
				}
				else
				{*/
					ReadVPKResult read_vpk_result = ReadVPK(search_paths[i], out List<VPKFile> read_vpk);
					if(read_vpk_result == ReadVPKResult.Success)
					{
						search_vpks[i] = read_vpk.ToDictionary(key => key.file_path, StringComparer.OrdinalIgnoreCase);
					}
					else
					{
						search_vpks[i] = new Dictionary<string, VPKFile>(StringComparer.OrdinalIgnoreCase);
						Log("Failed to read \""+search_paths[i]+"\" (reason: "+read_vpk_result.GetTextField()+")", LogType.Warning);
					}
				//}
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			ButtonPackData();
		}

		void ButtonPackData()
		{
			if(!File.Exists(input_vmf))
			{
				string message = Path.GetFileName(input_vmf)+" not found";
				if(!cmd)
				{
					MessageBox.Show(message+'.', Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					CmdCloseReason(message);
				}
				return;
			}
			bool bsp_exists;
			if(pack && (!(bsp_exists = File.Exists(input_bsp)) || !IsFileNotLocked(input_bsp, FileAccess.ReadWrite)))
			{
				string message = Path.GetFileName(input_bsp) + (!bsp_exists ? " not found" : " is occupied by another process");
				if(!cmd)
				{
					MessageBox.Show(message+'.', Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					CmdCloseReason(message);
				}
				return;
			}
			if(!IsSteamPath(steam_path))
			{
				string message = (!cmd ? "Steam directory isn't " + (steam_path == "" ? "specified" : "correct") + ". You can change it in the settings" : "Steam directory " + (steam_path == "" ? "not found" : "isn't correct"));
				if(!cmd)
				{
					if(MessageBox.Show(message+'.', Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
					{
						OpenSettings();
					}
				}
				else
				{
					CmdCloseReason(message);
				}
				return;
			}
			FindLibraryDirectoryResult find_library_result = FindLibraryDirectory(games_info[game].game_folder, games_info[game].game_root_folder, out library_path);
			if(find_library_result == FindLibraryDirectoryResult.LibraryFoldersVDFNotFound)
			{
				string message = "libraryfolders.vdf not found";
				if(!cmd)
				{
					MessageBox.Show(message+'.', Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					CmdCloseReason(message);
				}
				return;
			}
			else if(find_library_result == FindLibraryDirectoryResult.AppManifestNotFound)
			{
				string message = "Game-specific appmanifest_*.acf not found";
				if(!cmd)
				{
					MessageBox.Show(message+'.', Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					CmdCloseReason(message);
				}
				return;
			}
			if(!use_native_tools && games_info[game].vpk_paths.Count > 0/*!!this condition should've been removed if vpk.exe had been used to extract files*/ && !File.Exists(PathCombine(library_path, "steamapps/common", games_info[game].game_folder, "bin", "vpk.exe")))
			{
				string message = "vpk.exe not found";
				if(!cmd)
				{
					MessageBox.Show(message+'.', Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					CmdCloseReason(message);
				}
				return;
			}
			if((!use_native_tools && pack && !File.Exists(PathCombine(library_path, "steamapps/common", games_info[game].game_folder, "bin", "bspzip.exe"))) || (!pack && !cmd))
			{
				string message = (pack ? "bspzip.exe not found" + (!cmd ? ". " : "") : "") + (!cmd ? "Would you like to continue?\nIf you select 'Yes', the files won't be packed into the .bsp, but a file with a list of all detected custom assets will be saved in the cache folder" : "");
				if(!cmd)
				{
					DialogResult message_box = MessageBox.Show(message+'.', Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
					if(message_box == DialogResult.Yes)
					{
						checkBox1.Checked = false; //i.e. pack = false
					}
					else if(message_box == DialogResult.No)
					{
						return;
					}
				}
				else
				{
					CmdCloseReason(message);
					return;
				}
			}
			if(!cmd)
			{
				button1.Enabled =
				button2.Enabled =
				button3.Enabled =
				label1.Enabled =
				label2.Enabled =
				label3.Enabled =
				label4.Enabled =
				checkBox1.Enabled =
				checkBox2.Enabled =
				linkLabel2.Enabled = false;
				label5.Visible =
				progressBar1.Visible = true;
				SetProgress(3);
			}
			vmfs_found = new HashSet<string>(StringComparer.OrdinalIgnoreCase){input_vmf};
			vmfs_to_pack = new Queue<string>(new []{input_vmf});
			Thread thread_pack = new Thread(PackData);
			pack_thread = thread_pack;
			thread_pack.Start();
		}

		void PackData()
		{
			bool processing_base_map = vmfs_found.Count == 1;
			if(processing_base_map)
			{
				packing = true;
				notification_text = Constants.default_notification;

				if(log)
				{
					if(!Directory.Exists(cache_path))Directory.CreateDirectory(cache_path);
					File.WriteAllText(PathCombine(cache_path, vmf_name+".log"), "");
				}
				if(deferred_log.Count > 0)
				{
					for(int i = 0;i < deferred_log.Count;i++)
					{
						Log(deferred_log[i].Key, LogType.None);
						if(deferred_log[i].Value == LogType.Warning)warnings_amount++;
					}
					//deferred_log = new List<KeyValuePair<string, LogType>>(); //this line is commented out so that argument warnings are shown every time packing starts, not just the first time after startup
				}
				Log("Starting", print_timestamp: true);
				Log("Program version: "+GetProgramVersion().ToString());
				if(use_native_tools)Log("Native tool"+(pack ? "s" : "")+" will be used instead of official vpk.exe"+(pack ? " and bspzip.exe" : ""));
				Log("Game: "+games_info[game].game_folder+" ("+games_info[game].game_root_folder+")");
			}
			Log("VMF file: "+PathCombine(vmfs_to_pack.Peek()));
			if(processing_base_map)
			{
				if(pack)
				{
					Log("BSP file: "+PathCombine(input_bsp));
				}

				game_vpk_files = games_info[game].vpk_paths.Select(item => IsFullPath(item) ? PathCombine(item) : PathCombine(library_path, "steamapps/common", games_info[game].game_folder, item)).ToDictionary(key => key, value => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

				if(settings[(int)Settings.DetectCustomSearchPaths])
				{
					DetectSearchPaths();
					ReadSearchVpks();
				}

				string vpk_exe_path = PathCombine(library_path, "steamapps/common", games_info[game].game_folder, "bin", "vpk.exe");
				int vpk_number = 0;
				foreach(string game_vpk_path in game_vpk_files.Keys)
				{
					vpk_number++;
					if(!Path.GetExtension(game_vpk_path).EqualsCI(".vpk"))
					{
						Log("This file is not a VPK package: "+game_vpk_path, LogType.Warning);
						continue;
					}
					if(!File.Exists(game_vpk_path))
					{
						Log("Could not find VPK package: "+game_vpk_path, LogType.Warning);
						continue;
					}
					ConsoleText("Reading ("+((search_vpks?.Count ?? 0) + vpk_number)+"/"+((search_vpks?.Count ?? 0) + game_vpk_files.Count)+"): "+ShortenFileName(Path.GetFileName(game_vpk_path), 30));
					if(!use_native_tools)
					{
						CLAResult vpk_result = RunCLA(vpk_exe_path, "l \""+game_vpk_path+"\"");
						if(vpk_result.occurred_exception == null/* && string.IsNullOrWhiteSpace(vpk.error_data)*/)
						{
							game_vpk_files[game_vpk_path].UnionWith(vpk_result.output_data.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.RemoveEmptyEntries).AsParallel().Select(item => PathCombine(item) ?? ""));
						}
						else
						{
							Log("Failed to read \""+game_vpk_path+"\" (reason: "+/*(vpk.occured_exception != null ? */vpk_result.occurred_exception.GetDetailedMessage()/* : "unknown")*/+"; exit code: "+vpk_result.exit_code+")", LogType.Warning);
						}
					}
					else
					{
						ReadVPKResult read_vpk_result = ReadVPK(game_vpk_path, out List<VPKFile> read_vpk);
						if(read_vpk_result == ReadVPKResult.Success)
						{
							game_vpk_files[game_vpk_path].UnionWith(read_vpk.AsParallel().Select(item => item.file_path));
						}
						else
						{
							Log("Failed to read \""+game_vpk_path+"\" (reason: "+read_vpk_result.GetTextField()+"), try running without the '"+cmd_options[2]+"' option", LogType.Warning);
						}
					}
				}
			}

			Log("Reading the VMF file...");
			ConsoleText("Reading the VMF file...");

			HashList<string> custom_materials = new HashList<string>(StringComparer.OrdinalIgnoreCase);
			HashList<string> custom_models = new HashList<string>(StringComparer.OrdinalIgnoreCase);
			HashList<string> custom_sounds = new HashList<string>(StringComparer.OrdinalIgnoreCase);
			HashList<string> custom_scripts = new HashList<string>(StringComparer.OrdinalIgnoreCase);
			HashList<string> custom_configs = new HashList<string>(StringComparer.OrdinalIgnoreCase);
			HashList<string> custom_other_files = new HashList<string>(StringComparer.OrdinalIgnoreCase);

			HashSet<string> all_materials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> all_models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> all_sounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> all_scripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> all_configs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> all_other_files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			ProcessVMFFile(vmfs_to_pack.Peek(), true);
			if(extra_files[(int)ExtraFiles.MapCommentary] && processing_base_map)
			{
				string map_commentary = PathCombine("maps", vmf_name+"_commentary.txt");
				string map_commentary_full_path = FindFullFilePath(map_commentary, ExtractOption.DontExtract).full_file_path;
				if(map_commentary_full_path != null)
				{
					all_other_files.Add(map_commentary);
					custom_other_files.Add(map_commentary);
					Log(".txt (map commentary) file was detected");
					ProcessVMFFile(map_commentary_full_path, false);
				}
			}

			void AddSkyboxMaterials(string skybox_name)
			{
				string[] skybox_sides =
				{
					"ft",
					"bk",
					"lf",
					"rt",
					"dn",
					"up"
				};
				for(int i = 0;i < skybox_sides.Length;i++)
				{
					string current_skybox_side = VerifyExtension(PathCombine("materials/skybox", skybox_name + skybox_sides[i]), ".vmt");
					if(current_skybox_side != null && all_materials.Add(current_skybox_side))
					{
						if(FindFullFilePath(current_skybox_side).result != FindFullFilePathResult.FoundInGameVPKs)custom_materials.Add(current_skybox_side);
					}
				}
			}

			void ProcessScript(string found_script_path)
			{
				string script_name = PathCombine("scripts/vscripts", found_script_path);
				if(script_name != null)
				{
					if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"", ".nut"}.Contains(Path.GetExtension(found_script_path)))
					{
						script_name = VerifyExtension(script_name, ".nut");
						if(all_scripts.Add(script_name))
						{
							if(FindFullFilePath(script_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_scripts.Add(script_name);
						}
					}
				}
			}

			void ProcessVMFFile(string vmf_path, bool is_static)
			{
				DictionaryList<string, string> world_block = is_static ? GetBlockData(new ContentInfo{file_path = vmf_path}, "world", @"(?:detailmaterial|detailvbsp|skyname|material)", data_search_option: SearchOption.AllDirectories, parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues : null;
				List<BlockData> entity_blocks = GetBlockData(new ContentInfo{file_path = vmf_path}, "entity", data_search_option: SearchOption.AllDirectories, parent_block_search_option: SearchOption.TopDirectoryOnly, parent_block_search_option2: SearchOption2.AllOccurrences, search_mode: SearchMode.SearchEverything, hierarchize_block_data: true);

				if(world_block != null)
				{
					if(world_block.ContainsKey("detailmaterial"))
					{
						string detail_material_name = VerifyExtension(PathCombine("materials", world_block["detailmaterial"][0]), ".vmt");
						if(detail_material_name != null && all_materials.Add(detail_material_name))
						{
							if(FindFullFilePath(detail_material_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_materials.Add(detail_material_name);
						}
					}
					if(world_block.ContainsKey("detailvbsp"))
					{
						string detail_vbsp_name = PathCombine(world_block["detailvbsp"][0]);
						if(detail_vbsp_name != null && all_other_files.Add(detail_vbsp_name))
						{
							if(FindFullFilePath(detail_vbsp_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_other_files.Add(detail_vbsp_name);
						}
					}
					if(world_block.ContainsKey("skyname"))
					{
						AddSkyboxMaterials(world_block["skyname"][0]);
					}
					if(world_block.ContainsKey("material"))
					{
						for(int i = 0;i < world_block["material"].Count;i++)
						{
							string material_name = VerifyExtension(PathCombine("materials", world_block["material"][i]), ".vmt");
							if(material_name != null && all_materials.Add(material_name))
							{
								if(FindFullFilePath(material_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_materials.Add(material_name);
							}
						}
					}
				}

				if(entity_blocks != null)
				{
					void ProcessMaterial(string current_key, string current_value, string classname, string current_value_extension, Tuple<RootFolder, KeyCustomLogic> key_options_override = null)
					{
						KeyParameters key_parameters = key_options_override == null ? GetKeyParameters(Constants.VMF_material_keys, current_key) : default;
						string material_name = key_options_override == null ? VerifyRootFolder(current_value, "materials", key_parameters, classname) : VerifyRootFolder(current_value, "materials", key_options_override.Item1);
						if(material_name != null)
						{
							KeyCustomLogic current_logic = key_options_override == null ? GetVMFKeyCustomLogic(key_parameters, classname) : key_options_override.Item2;
							if(current_logic.HasFlag(KeyCustomLogic.Sprite))
							{
								if(!new HashSet<string>(StringComparer.OrdinalIgnoreCase){"", ".vmt", ".spr"}.Contains(current_value_extension))return;
							}
							material_name = VerifyExtension(material_name, ".vmt");
							if(all_materials.Add(material_name))
							{
								if(FindFullFilePath(material_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_materials.Add(material_name);
							}
						}
					}

					int previous_percentage_entities = !cmd ? progressBar1.Value : 0;
					for(int i = 0;i < entity_blocks.Count;i++)
					{
						if(!cmd)
						{
							int current_percentage_vmf = Round((float)i / entity_blocks.Count * 100);
							if(current_percentage_vmf > previous_percentage_entities) //for optimization purpose
							{
								ConsoleText("Reading the VMF file... ("+current_percentage_vmf+"%)");
								if(is_static)SetProgress(Round(Remap(current_percentage_vmf, 0, 100, 3, pack ? 50 : 60)));
								previous_percentage_entities = current_percentage_vmf;
							}
						}

						entity_blocks[i].keyvalues.TryGetValue("classname", out string classname);

						//checks common keyvalues
						for(int k = 0;k < entity_blocks[i].keyvalues.Count;k++)
						{
							string current_key = entity_blocks[i].keyvalues[k].Key;
							string current_value = entity_blocks[i].keyvalues[k].Value[0];
							string current_value_extension = null;
							try
							{
								current_value_extension = Path.GetExtension(current_value);
							}
							catch{}

							if(current_key.EqualsCI("vscripts"))
							{
								string[] scripts = current_value.Split(' ');
								for(int j = 0;j < scripts.Length;j++)
								{
									ProcessScript(scripts[j]);
								}
							}
							else if(current_key.EqualsCI("vehiclescript"))
							{
								if(all_other_files.Add(current_value)) //already has "scripts/"
								{
									if(FindFullFilePath(current_value).result != FindFullFilePathResult.FoundInGameVPKs)custom_other_files.Add(current_value);
								}
							}
							else if(Regex.Match(current_key, VMF_model_keys_pattern, RegexOptions.IgnoreCase).Success)
							{
								KeyParameters key_parameters = GetKeyParameters(Constants.VMF_model_keys, current_key);
								if((key_parameters.classnames_exceptions != null && key_parameters.classnames_exceptions.TryGetValue(classname, out Tuple<RootFolder, KeyCustomLogic> key_options) && key_options.Item2.HasFlag(KeyCustomLogic.ProcessAsMaterial)) || (current_value_extension?.Length > 0 && key_parameters.extensions_exceptions != null && key_parameters.extensions_exceptions.TryGetValue(current_value_extension.Remove(0, 1), out key_options) && key_options.Item2.HasFlag(KeyCustomLogic.ProcessAsMaterial)))
								{
									ProcessMaterial(current_key, current_value, classname, current_value_extension, new Tuple<RootFolder, KeyCustomLogic>(key_options.Item1, key_options.Item2 ^ KeyCustomLogic.ProcessAsMaterial));
								}
								else
								{
									if(!classname.EqualsCI("filter_activator_model"))
									{
										string model_name = VerifyRootFolder(current_value, "models", key_parameters, classname);
										if(model_name != null && all_models.Add(model_name))
										{
											if(FindFullFilePath(model_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_models.Add(model_name);
										}
									}
								}
							}
							else if(Regex.Match(current_key, VMF_material_keys_pattern, RegexOptions.IgnoreCase).Success)
							{
								if(!current_key.EqualsCI("material") || !new HashSet<string>(StringComparer.OrdinalIgnoreCase){"func_breakable", "func_breakable_surf"}.Contains(classname)) //prevents func_breakable*'s material type from being treated as a VMT material
								{
									ProcessMaterial(current_key, current_value, classname, current_value_extension);
								}
							}
							else if(current_value_extension?.Length > 0 && Constants.sound_extensions.Contains(current_value_extension.Remove(0, 1)))
							{
								string sound_name = PathCombine("sound", current_value.TrimStart(Constants.sound_characters));
								if(sound_name != null && all_sounds.Add(sound_name))
								{
									if(FindFullFilePath(sound_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_sounds.Add(sound_name);
								}
							}
						}

						for(int k = 0;k < entity_blocks[i].children_blocks.Count;k++)
						{
							if(entity_blocks[i].children_blocks[k].block_name.EqualsCI("solid")) //gets materials from solids within entities
							{
								for(int j = 0;j < entity_blocks[i].children_blocks[k].children_blocks.Count;j++)
								{
									if(entity_blocks[i].children_blocks[k].children_blocks[j].block_name.EqualsCI("side") && entity_blocks[i].children_blocks[k].children_blocks[j].keyvalues.TryGetValue("material", out string material_name))
									{
										material_name = VerifyExtension(PathCombine("materials", material_name), ".vmt");
										if(material_name != null && all_materials.Add(material_name))
										{
											if(FindFullFilePath(material_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_materials.Add(material_name);
										}
									}
								}
							}
							else if(entity_blocks[i].children_blocks[k].block_name.EqualsCI("connections")) //checks outputs
							{
								foreach(KeyValuePair<string, string> output in entity_blocks[i].children_blocks[k].keyvalues)
								{
									string[] output_split = output.Value.Split((char)0x1b);
									if(output_split.Length == 1)output_split = output.Value.Split(',');
									if(output_split.Length < 3)continue;
									string input = output_split[1];
									string parameter = output_split[2];
									if(input.EqualsCI("RunScriptFile"))
									{
										ProcessScript(parameter);
									}
									else if(input.EqualsCI("RunScriptCode"))
									{
										foreach(string script_string in GetScriptStrings(new ContentInfo{file_content = parameter.Replace('`', '\"')})) //should it replace backticks only for TF2 ?!!
										{
											ProcessScriptString(script_string);
										}
									}
									else if(input.EqualsCI("Command"))
									{
										List<List<string>> split_commands = SplitConsoleCommand(new ContentInfo{file_content = parameter});
										for(int j = 0;j < split_commands.Count;j++)
										{
											ProcessSplitConsoleCommand(split_commands[j]);
										}
									}
								}
							}
						}

						Dictionary<string, Tuple<List<string>, Func<bool>>> hardcoded_materials = new Dictionary<string, Tuple<List<string>, Func<bool>>>(StringComparer.OrdinalIgnoreCase)
						{
							{"env_bubbles", new (new List<string>{"materials/sprites/bubble.vmt"}, () => true)},
							{"env_dustpuff", new (new List<string>{"materials/particle/particle_smokegrenade.vmt", "materials/particle/particle_noisesphere.vmt"}, () => true)},
							{"env_embers", new (new List<string>{"materials/particle/fire.vmt"}, () => true)},
							{"env_funnel", new (new List<string>{"materials/sprites/flare6.vmt"}, () => true)},
							{"env_lightglow", new (new List<string>{"materials/sprites/light_glow02_add_noz.vmt"}, () => true)},
							{"env_spark", new (new List<string>{"materials/sprites/glow01.vmt"}, () => true)},
							{"env_steam", new (new List<string>{"materials/particle/particle_smokegrenade.vmt", "materials/sprites/heatwave.vmt"}, () => true)},
							{"func_dustmotes", new (new List<string>{"materials/particle/sparkles.vmt"}, () => true)},
							{"func_dustcloud", new (new List<string>{"materials/particle/sparkles.vmt"}, () => true)},
							{"func_precipitation", new (new List<string>{"materials/effects/fleck_ash1.vmt", "materials/effects/fleck_ash2.vmt", "materials/effects/fleck_ash3.vmt", "materials/effects/ember_swirling001.vmt"}, () => entity_blocks[i].keyvalues.TryGetValue("preciptype", out string precip_type) && precip_type == "2")}
						};

						//checks classname-specific keyvalues
						if(hardcoded_materials.ContainsKey(classname))
						{
							if(hardcoded_materials[classname].Item2())
							{
								custom_materials.AddRanges(hardcoded_materials[classname].Item1.Where(item => all_materials.Add(item) && FindFullFilePath(item).result != FindFullFilePathResult.FoundInGameVPKs));
							}
						}
						else if(classname.EqualsCI("info_enemy_terrorist_spawn"))
						{
							if(extra_files[(int)ExtraFiles.BotsBehavior])
							{
								if(entity_blocks[i].keyvalues.TryGetValue("behavior_tree_file", out string kv3_name))
								{
									kv3_name = PathCombine(kv3_name); //already has "scripts/"
									if(kv3_name != null && all_other_files.Add(kv3_name))
									{
										if(FindFullFilePath(kv3_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_other_files.Add(kv3_name);
									}
								}
							}
						}
						else if(classname.EqualsCI("skybox_swapper"))
						{
							if(entity_blocks[i].keyvalues.TryGetValue("skyboxname", out string skybox_name))
							{
								AddSkyboxMaterials(skybox_name);
							}
						}
						else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"color_correction", "color_correction_volume"}.Contains(classname))
						{
							if(entity_blocks[i].keyvalues.TryGetValue("filename", out string raw_name))
							{
								raw_name = PathCombine(raw_name); //already has "materials/"
								if(raw_name != null && all_other_files.Add(raw_name))
								{
									if(FindFullFilePath(raw_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_other_files.Add(raw_name);
								}
							}
						}
						else if(classname.EqualsCI("point_worldtext"))
						{
							if(entity_blocks[i].keyvalues.TryGetValue("font", out string font_material_name))
							{
								if(!int.TryParse(font_material_name, out _))
								{
									font_material_name = VerifyExtension(PathCombine("materials", font_material_name), ".vmt");
									if(font_material_name != null && all_materials.Add(font_material_name))
									{
										if(FindFullFilePath(font_material_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_materials.Add(font_material_name);
									}
								}
							}
						}
						else if(classname.EqualsCI("env_effectscript"))
						{
							if(entity_blocks[i].keyvalues.TryGetValue("scriptfile", out string script_name))
							{
								script_name = PathCombine(script_name); //already has "scripts/"
								if(script_name != null && all_other_files.Add(script_name))
								{
									if(FindFullFilePath(script_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_other_files.Add(script_name);
								}
							}
						}
						else if(classname.EqualsCI("func_instance"))
						{
							if(settings[(int)Settings.DetectVMFsInFuncInstances])
							{
								if(entity_blocks[i].keyvalues.TryGetValue("file", out string file_name))
								{
									try //handles the "invalid path" exception
									{
										file_name = VerifyExtension(PathCombine(Path.GetFullPath(PathCombine(Path.GetDirectoryName(input_vmf), file_name))), ".vmf");
										if(File.Exists(file_name))
										{
											if(vmfs_found.Add(file_name))vmfs_to_pack.Enqueue(file_name);
										}
										else
										{
											Log("Could not find VMF file specified in func_instance: "+file_name, LogType.Warning);
										}
									}
									catch{}
								}
							}
						}
					}
				}
			}

			Log("Finished reading the VMF file");

			int missing_custom_scripts_amount = 0; //this variable is used to correctly log the amount of custom files, otherwise missing custom scripts won't be taken into account
			if(settings[(int)Settings.DetectFilesInScripts])
			{
				for(int i = 0;i < custom_scripts.Count;i++)
				{
					FullFilePath script_info = FindFullFilePath(custom_scripts[i]);
					if(script_info.result == FindFullFilePathResult.Found)
					{
						ConsoleText("Reading ("+(i + 1)+"/"+(custom_scripts.Count + custom_configs.Count)+"): "+ShortenFileName(Path.GetFileName(custom_scripts[i]), 30));
						foreach(string script_string in GetScriptStrings(new ContentInfo { file_path = script_info.full_file_path }))
						{
							ProcessScriptString(script_string);
						}
					}
					else
					{
						if(script_info.result == FindFullFilePathResult.NotFound)Log("Could not find file: "+custom_scripts[i], LogType.Warning);
						custom_scripts.RemoveAt(i);
						i--;
						missing_custom_scripts_amount++;
					}
				}
				for(int i = 0;i < custom_configs.Count;i++)
				{
					FullFilePath config_info = FindFullFilePath(custom_configs[i]);
					if(config_info.result == FindFullFilePathResult.Found)
					{
						ConsoleText("Reading ("+(custom_scripts.Count + i + 1)+"/"+(custom_scripts.Count + custom_configs.Count) +"): "+ShortenFileName(Path.GetFileName(custom_configs[i]), 30));
						List<List<string>> split_commands = SplitConsoleCommand(new ContentInfo{file_path = config_info.full_file_path});
						for(int j = 0;j < split_commands.Count;j++)
						{
							ProcessSplitConsoleCommand(split_commands[j]);
						}
					}
					else
					{
						if(config_info.result == FindFullFilePathResult.NotFound)Log("Could not find file: "+custom_configs[i], LogType.Warning);
						custom_configs.RemoveAt(i);
						i--;
						missing_custom_scripts_amount++;
					}
				}
			}

			void ProcessScriptString(string script_string)
			{
				foreach(Match file in Regex.Matches(script_string, script_master_pattern, RegexOptions.IgnoreCase))
				{
					string file_extension = file.Groups["extension"].Value;
					if(file_extension.EqualsCI(".mdl"))
					{
						string model_name = PathCombine(file.Groups["path"].Value); //already has "models/"
						if(model_name != null && all_models.Add(model_name))
						{
							if(FindFullFilePath(model_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_models.Add(model_name);
						}
					}
					else if(file_extension.EqualsCI(".nut"))
					{
						string script_name = PathCombine("scripts/vscripts", file.Groups["path"].Value);
						if(script_name != null && all_scripts.Add(script_name))
						{
							if(FindFullFilePath(script_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_scripts.Add(script_name);
						}
					}
					else if(file_extension.EqualsCI(".cfg"))
					{
						string config_name = PathCombine("cfg", file.Groups["path"].Value);
						if(config_name != null && all_scripts.Add(config_name))
						{
							if(FindFullFilePath(config_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_configs.Add(config_name);
						}
					}
					else if(Constants.sound_extensions.Contains(file_extension.Remove(0, 1)))
					{
						string sound_name = PathCombine("sound", file.Groups["path"].Value);
						if(sound_name != null && all_sounds.Add(sound_name))
						{
							if(FindFullFilePath(sound_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_sounds.Add(sound_name);
						}
					}
					else/* if(file_extension.EqualsCI(".kv3"))*/
					{
						string kv3_name = PathCombine(file.Groups["path"].Value); //already has "scripts/"
						if(kv3_name != null && all_other_files.Add(kv3_name))
						{
							if(FindFullFilePath(kv3_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_other_files.Add(kv3_name);
						}
					}
				}
			}

			void ProcessSplitConsoleCommand(List<string> split_command)
            {
				string command = split_command[0];
				if(split_command.Count > 1)
				{
					string first_argument = split_command[1];
					if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"script_execute", "script_execute_client", "script_reload_code"}.Contains(command))
					{
						ProcessScript(first_argument);
					}
					else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"script", "script_client"}.Contains(command))
					{
						foreach(string script_string in GetScriptStrings(new ContentInfo{file_content = first_argument}))
						{
							ProcessScriptString(script_string);
						}
					}
					else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"exec", "execifexists", "execwithwhitelist"}.Contains(command))
					{
						string cofig_name = PathCombine("cfg", first_argument);
						if(cofig_name != null)
						{
							if(Path.GetExtension(cofig_name) == "")cofig_name += ".cfg";
							if(all_scripts.Add(cofig_name))
							{
								if(FindFullFilePath(cofig_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_configs.Add(cofig_name);
							}
						}
					}
					else if(command.EqualsCI("mp_bot_ai_bt"))
					{
						if(extra_files[(int)ExtraFiles.BotsBehavior])
						{
							string kv3_name = PathCombine(first_argument); //already has "scripts/"
							if(kv3_name != null && all_other_files.Add(kv3_name))
							{
								if(FindFullFilePath(kv3_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_other_files.Add(kv3_name);
							}
						}
					}
					else if(command.EqualsCI("sv_skyname"))
					{
						AddSkyboxMaterials(first_argument);
					}
					else if(new HashSet<string>(StringComparer.OrdinalIgnoreCase){"play", "play_hrtf", "playflush", "playvol"}.Contains(command))
                    {
						string sound_name = PathCombine("sound", first_argument.TrimStart(Constants.sound_characters));
						if(sound_name != null)
						{
							string sound_extension = Path.GetExtension(sound_name);
							if(sound_extension.Length > 0 && Constants.sound_extensions.Contains(sound_extension.Remove(0, 1)) && all_sounds.Add(sound_name))
							{
								if(FindFullFilePath(sound_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_sounds.Add(sound_name);
							}
						}
                    }
				}
            }

			HashList<string> current_custom_files;
			if(custom_materials.Count + custom_models.Count > 0)
			{
				List<List<string>> custom_textures = new List<List<string>>(custom_materials.Count);
				HashSet<string> all_textures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				int missing_custom_materials_amount = 0; //this variable is used to correctly log the amount of custom files, otherwise missing custom textures won't be taken into account

				DetectTexturesInMaterials(ref custom_materials, ref custom_textures, ref all_materials, ref all_textures, ref missing_custom_materials_amount);

				current_custom_files = new HashList<string>(custom_materials.Count * 2, StringComparer.OrdinalIgnoreCase);
				int custom_textures_amount = 0;
				for(int i = 0;i < custom_materials.Count;i++)
				{
					current_custom_files.Add(custom_materials[i]);
					current_custom_files.AddRanges(custom_textures[i]);
					custom_textures_amount += custom_textures[i].Count;
				}

				LogFilesInfo(all_materials.Count, custom_materials.Count + missing_custom_materials_amount, "material{0}");
				LogFilesInfo(all_textures.Count, custom_textures_amount, "texture{0}");
				LogFilesInfo(all_models.Count, custom_models.Count, "model{0}");
				LogFilesInfo(all_sounds.Count, custom_sounds.Count, "sound{0}");
				LogFilesInfo(all_scripts.Count, custom_scripts.Count + custom_configs.Count + missing_custom_scripts_amount, "script{0}/config{0}");
				LogFilesInfo(all_other_files.Count, custom_other_files.Count, "other file{0} (.kv3, .raw, detail vbsp, etc.)");

				List<HashList<string>> model_material_names = new List<HashList<string>>(custom_models.Count);
				List<HashList<string>> model_material_paths = new List<HashList<string>>(custom_models.Count);
				HashList<string> custom_included_models = new HashList<string>(StringComparer.OrdinalIgnoreCase);
				HashSet<string> all_included_models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				HashSet<string> all_internal_models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				int custom_internal_models_amount = 0;
				if(custom_models.Count > 0)
				{
					Log("Reading models...");

					int previous_percentage_models = !cmd ? progressBar1.Value : 0;
					for(int i = 0;i < custom_models.Count;i++)
					{
						if(!cmd)
						{
							int current_percentage_models = Round((float)i / custom_models.Count * 100);
							if(current_percentage_models > previous_percentage_models) //for optimization purpose
							{
								SetProgress(Round(Remap(current_percentage_models, 0, 100, pack ? 50 : 60, pack ? 80 : 90)));
								previous_percentage_models = current_percentage_models;
							}
						}

						FullFilePath mdl_info = FindFullFilePath(custom_models[i]);
						if(mdl_info.result == FindFullFilePathResult.Found)
						{
							ConsoleText("Reading ("+(i + 1)+"/"+custom_models.Count+"): "+ShortenFileName(Path.GetFileName(mdl_info.full_file_path), 30));

							FileStream file_stream = new FileStream(mdl_info.full_file_path, FileMode.Open, FileAccess.Read, FileShare.Read);
							BinaryReader binary_reader = new BinaryReader(file_stream);

							MdlHeader mdl_header = binary_reader.ReadStruct<MdlHeader>();

							HashList<string> textures = new HashList<string>(mdl_header.texture_count, StringComparer.OrdinalIgnoreCase);
							file_stream.Seek(mdl_header.texture_offset, SeekOrigin.Begin);
							for(int k = 0;k < mdl_header.texture_count;k++)
							{
								long position_begin = file_stream.Position;

								int mat_name_offset = binary_reader.ReadInt32();

								long after_name_offset = file_stream.Position;

								if(mat_name_offset != 0)
								{
									file_stream.Seek(position_begin + mat_name_offset, SeekOrigin.Begin);

									string texture_name = binary_reader.ReadNullTerminatedString();
									textures.Add(texture_name);
								}

								file_stream.Seek(after_name_offset + (4 * 15), SeekOrigin.Begin);
							}

							HashList<string> texture_paths = new HashList<string>(mdl_header.texture_dir_count, StringComparer.OrdinalIgnoreCase);
							file_stream.Seek(mdl_header.texture_dir_offset, SeekOrigin.Begin);
							for(int k = 0;k < mdl_header.texture_dir_count;k++)
							{
								int current_path_offset = binary_reader.ReadInt32();

								long path_offset = file_stream.Position;

								if(current_path_offset != 0)
								{
									file_stream.Seek(current_path_offset, SeekOrigin.Begin);

									string texture_path = PathCombine(binary_reader.ReadNullTerminatedString())?.TrimEnd('/');
									if(texture_path != null)
									{
										texture_paths.Add(texture_path);
									}
								}

								file_stream.Seek(path_offset, SeekOrigin.Begin);
							}

							file_stream.Seek(mdl_header.include_models_offset, SeekOrigin.Begin);
							for(int k = 0;k < mdl_header.include_models_count;k++)
							{
								long position_begin = file_stream.Position;

								//int label_offset = binary_reader.ReadInt32();
								file_stream.Seek(1 * 4, SeekOrigin.Current);
								int included_model_name_offset = binary_reader.ReadInt32();

								long after_name_offset = file_stream.Position;

								if(included_model_name_offset != 0)
								{
									file_stream.Seek(position_begin + included_model_name_offset, SeekOrigin.Begin);

									string included_model = PathCombine(binary_reader.ReadNullTerminatedString()); //already has "models/"
									if(included_model != null && all_included_models.Add(included_model))
									{
										if(FindFullFilePath(included_model).result != FindFullFilePathResult.FoundInGameVPKs)custom_included_models.Add(included_model);
									}
								}

								file_stream.Seek(after_name_offset, SeekOrigin.Begin);
							}

							if(mdl_header.keyvalue_size > 0)
							{
								file_stream.Seek(mdl_header.keyvalue_offset, SeekOrigin.Begin);
								DictionaryList<string, string> damage_models = GetBlockData(new ContentInfo{file_content = binary_reader.ReadString(mdl_header.keyvalue_size - 1)}, "door_options", @"damage\d*", data_search_option: SearchOption.AllDirectories)?[0]?.keyvalues;
								if(damage_models != null)
								{
									for(int k = 0;k < damage_models.Count;k++)
									{
										string damage_model_name = VerifyExtension(PathCombine("models", damage_models[k].Value[0]), ".mdl");
										if(damage_model_name != null && all_internal_models.Add(damage_model_name))
										{
											if(FindFullFilePath(damage_model_name).result != FindFullFilePathResult.FoundInGameVPKs)
											{
												custom_internal_models_amount++;
												custom_models.Add(damage_model_name);
											}
										}
									}
								}
							}

							binary_reader.Dispose();
							file_stream.Dispose();

							string phy_path = FindFullFilePath(Path.ChangeExtension(custom_models[i], ".phy")).full_file_path;
							if(phy_path != null)
							{
								DictionaryList<string, string> break_models = GetBlockData(new ContentInfo{file_content = ReadPhyTextData(phy_path)}, "break", @"(?:model|ragdoll)", data_search_option: SearchOption.AllDirectories, parent_block_search_option2: SearchOption2.AllOccurrences)?[0]?.keyvalues; //data_search_option and parent_block_search_option are set to these values just in case
								if(break_models != null)
								{
									foreach(KeyValuePair<string, string> current_keyvalue in break_models)
									{
										string break_model_name = VerifyExtension(PathCombine("models", current_keyvalue.Value), ".mdl");
										if(break_model_name != null && all_internal_models.Add(break_model_name))
										{
											if(FindFullFilePath(break_model_name).result != FindFullFilePathResult.FoundInGameVPKs)
											{
												custom_internal_models_amount++;
												custom_models.Add(break_model_name);
											}
										}
									}
								}
							}

							model_material_names.Add(textures);
							model_material_paths.Add(texture_paths);
						}
						else
						{
							if(mdl_info.result == FindFullFilePathResult.NotFound)Log("Could not find file: "+custom_models[i], LogType.Warning);
							custom_models.RemoveAt(i);
							i--;
						}
					}

					custom_models.AddRanges(custom_included_models); //$includemodel doesn't use the model's materials

					Log("Finished reading models");
					LogFilesInfo(all_included_models.Count + all_internal_models.Count, custom_included_models.Count + custom_internal_models_amount, "\"internal\" model{0} (gib, door damage or $includemodel)");

					ConsoleText("Detecting model-related files...");
					List<List<string>> model_related_files = new List<List<string>>(custom_models.Count);
					DetectModelRelatedFiles(custom_models, ref model_related_files);
					for(int i = 0;i < custom_models.Count;i++)
					{
						current_custom_files.Add(custom_models[i]);
						if(model_related_files[i] != null)
						{
							current_custom_files.AddRanges(model_related_files[i]);
						}
					}

					for(int i = 0;i < /*custom_models*/model_material_names.Count;i++) //model_material_names is used here to not process included models
					{
						for(int k = 0;k < model_related_files[i].Count;k++)
						{
							if(!Path.GetExtension(model_related_files[i][k]).EqualsCI(".vtx"))continue;
							string vtx_path = FindFullFilePath(model_related_files[i][k]).full_file_path;
							if(vtx_path != null)
							{
								try
								{
									using FileStream file_stream = new FileStream(vtx_path, FileMode.Open, FileAccess.Read, FileShare.Read);
									using BinaryReader binary_reader = new BinaryReader(file_stream);

									VtxHeader vtx_header = binary_reader.ReadStruct<VtxHeader>();

									file_stream.Seek(vtx_header.material_replacement_list_offset, SeekOrigin.Begin);
									for(int j = 0;j < vtx_header.num_lods;j++)
									{
										int replacement_count = binary_reader.ReadInt32();
										int replacement_offset = binary_reader.ReadInt32();

										long material_replacement_list_end_offset = file_stream.Position;

										file_stream.Seek(material_replacement_list_end_offset - 8 + replacement_offset, SeekOrigin.Begin);
										for(int l = 0;l < replacement_count;l++)
										{
											short material_index = binary_reader.ReadInt16();
											int replacement_material_name_offset = binary_reader.ReadInt32();

											long material_replacement_end_offset = file_stream.Position;

											if(replacement_material_name_offset != 0)
											{
												file_stream.Seek(material_replacement_end_offset - 6 + replacement_material_name_offset, SeekOrigin.Begin);
												model_material_names[i].Add(binary_reader.ReadNullTerminatedString());
											}

											file_stream.Seek(material_replacement_end_offset, SeekOrigin.Begin);
										}

										file_stream.Seek(material_replacement_list_end_offset, SeekOrigin.Begin);
									}
								}
								catch{}
							}
						}
					}

					ConsoleText("Finding model materials...");

					HashList<string> custom_model_materials = new HashList<string>(StringComparer.OrdinalIgnoreCase);
					HashSet<string> all_model_materials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					for(int i = 0;i < /*custom_models*/model_material_names.Count;i++) //model_material_names is used here to not process included models
					{
						for(int k = 0;k < model_material_names[i].Count;k++)
						{
							bool found = false;
							for(int j = 0;j < model_material_paths[i].Count;j++)
							{
								string current_material_path = PathCombine("materials", model_material_paths[i][j], model_material_names[i][k]+".vmt");
								if(current_material_path != null)
								{
									FindFullFilePathResult file_info_result = FindFullFilePath(current_material_path).result;
									if(file_info_result == FindFullFilePathResult.Found)
									{
										all_model_materials.Add(current_material_path);
										custom_model_materials.Add(current_material_path);
										found = true;
										break;
									}
									else if(file_info_result == FindFullFilePathResult.FoundInGameVPKs || file_info_result == FindFullFilePathResult.FailedToExtract)
									{
										all_model_materials.Add(current_material_path);
										found = true;
										break;
									}
								}
							}
							if(!found)
							{
								Log("Could not find model's ("+custom_models[i]+") material: "+model_material_names[i][k]+".vmt", LogType.Warning);
							}
						}
					}

					List<List<string>> custom_model_textures = new List<List<string>>(custom_model_materials.Count);
					HashSet<string> all_model_textures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					int missing_custom_model_materials_amount = 0; //this variable is used to correctly log the amount of custom files, otherwise missing custom model textures won't be taken into account

					DetectTexturesInMaterials(ref custom_model_materials, ref custom_model_textures, ref all_model_materials, ref all_model_textures, ref missing_custom_model_materials_amount);

					int custom_model_textures_amount = 0;
					for(int i = 0;i < custom_model_materials.Count;i++)
					{
						current_custom_files.Add(custom_model_materials[i]);
						current_custom_files.AddRanges(custom_model_textures[i]);
						custom_model_textures_amount += custom_model_textures[i].Count;
					}

					LogFilesInfo(all_model_materials.Count, custom_model_materials.Count + missing_custom_model_materials_amount, "model material{0}");
					LogFilesInfo(all_model_textures.Count, custom_model_textures_amount, "model texture{0}");
				}

				SetProgress(pack ? 80 : 90);
			}
			else
			{
				current_custom_files = new HashList<string>(StringComparer.OrdinalIgnoreCase);
				LogFilesInfo(all_materials.Count, custom_materials.Count, "material{0}");
				LogFilesInfo(all_models.Count, custom_models.Count, "model{0}");
				LogFilesInfo(all_sounds.Count, custom_sounds.Count, "sound{0}");
				LogFilesInfo(all_scripts.Count, custom_scripts.Count + custom_configs.Count + missing_custom_scripts_amount, "script{0}/config{0}");
				LogFilesInfo(all_other_files.Count, custom_other_files.Count, "other file{0} (.kv3, .raw, detail vbsp, etc.)");
			}
			current_custom_files.AddRanges(custom_sounds, custom_scripts, custom_configs, custom_other_files);

			if(processing_base_map)
			{
				ConsoleText("Detecting extra files...");
				string nav = PathCombine("maps", vmf_name+".nav");
				string ain = PathCombine("maps/graphs", vmf_name+".ain");
				string map_description = PathCombine("maps", vmf_name+".txt");
				string soundscape = PathCombine("scripts", "soundscapes_"+vmf_name+".txt");
				string soundscript = PathCombine("maps", vmf_name+"_level_sounds.txt");
				string soundcache = PathCombine("maps/soundcache", vmf_name+".cache");
				string map_retake = PathCombine("maps", vmf_name+"_retake.txt");
				string map_cameras = PathCombine("maps", vmf_name+"_cameras.txt");
				string map_story = PathCombine("maps", vmf_name+"_story.txt");
				string[] particles_manifests =
				{
					PathCombine("particles", "particles_manifest.txt"),
					PathCombine("maps", vmf_name+"_particles.txt")
				};
				string radar_info = PathCombine("resource/overviews", vmf_name+".txt");
				string map_icon = PathCombine("materials/panorama/images/map_icons", "map_icon_"+vmf_name+".svg");
				string map_background = PathCombine("materials/panorama/images/map_icons/screenshots/1080p", vmf_name+".png");
				string kv = PathCombine("maps", vmf_name+".kv");
				string dz_spawn_mask = PathCombine("maps", vmf_name+"_spawnmask.png");
				string dz_deployment_map = PathCombine("materials/panorama/images/survival/spawnselect", "map_"+vmf_name+".png");
				string dz_tablet_map = PathCombine("materials/models/weapons/v_models/tablet", "tablet_radar_"+vmf_name+".vtf");
				if(extra_files[(int)ExtraFiles.NavigationMesh] && FindFullFilePath(nav, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(nav);
					Log(".nav (navigation mesh) file was detected");
				}
				if(extra_files[(int)ExtraFiles.InfoNode] && FindFullFilePath(ain, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(ain);
					Log(".ain (info_node) file was detected");
				}
				if(extra_files[(int)ExtraFiles.MapDescription] && FindFullFilePath(map_description, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(map_description);
					Log(".txt (map description) file was detected");
				}
				string soundscape_full_path = null;
				string soundscript_full_path = null;
				if((extra_files[(int)ExtraFiles.Soundscape] && (soundscape_full_path = FindFullFilePath(soundscape).full_file_path) != null) || (extra_files[(int)ExtraFiles.SoundScript] && (soundscript_full_path = FindFullFilePath(soundscript).full_file_path) != null)) //can be extracted
				{
					if(extra_files[(int)ExtraFiles.Soundscape] && soundscape_full_path != null)
					{
						current_custom_files.Add(soundscape);
						Log(".txt (soundscape) file was detected");
					}
					if(extra_files[(int)ExtraFiles.SoundScript] && soundscript_full_path != null)
					{
						current_custom_files.Add(soundscript);
						Log(".txt (soundscript) file was detected");
					}
					HashList<string> custom_soundscape_soundscript_sounds = new HashList<string>(StringComparer.OrdinalIgnoreCase);
					HashSet<string> all_soundscape_soundscript_sounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					DictionaryList<string, string> sound_files = new DictionaryList<string, string>
					(
						GetBlockData(new ContentInfo{file_path = soundscape_full_path}, keyvalue_name_pattern: "wave", data_search_option: SearchOption.AllDirectories, parent_block_search_option2: SearchOption2.AllOccurrences)?[0]?.keyvalues,
						GetBlockData(new ContentInfo{file_path = soundscript_full_path}, keyvalue_name_pattern: "wave", data_search_option: SearchOption.AllDirectories, parent_block_search_option2: SearchOption2.AllOccurrences)?[0]?.keyvalues
					);
					if(sound_files != null)
					{
						for(int k = 0;k < sound_files[0].Value.Count;k++)
						{
							string sound_name = PathCombine("sound", sound_files[0].Value[k].TrimStart(Constants.sound_characters));
							if(sound_name != null)
							{
								string sound_extension = Path.GetExtension(sound_name);
								if(sound_extension.Length > 0 && Constants.sound_extensions.Contains(sound_extension.Remove(0, 1)) && all_soundscape_soundscript_sounds.Add(sound_name))
								{
									if(FindFullFilePath(sound_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_soundscape_soundscript_sounds.Add(sound_name);
								}
							}
						}
					}
					if((extra_files[(int)ExtraFiles.Soundscape] && soundscape_full_path != null) || (extra_files[(int)ExtraFiles.SoundScript] && soundscript_full_path != null))
					{
						LogFilesInfo(all_soundscape_soundscript_sounds.Count, custom_soundscape_soundscript_sounds.Count, "soundscape/soundscript sound{0}");

						for(int i = 0;i < custom_soundscape_soundscript_sounds.Count;i++)
						{
							FindFullFilePathResult file_info_result = FindFullFilePath(custom_soundscape_soundscript_sounds[i]).result; //can be extracted
							if(file_info_result != FindFullFilePathResult.Found)
							{
								if(file_info_result == FindFullFilePathResult.NotFound)Log("Could not find file: "+custom_soundscape_soundscript_sounds[i], LogType.Warning);
								custom_soundscape_soundscript_sounds.RemoveAt(i);
								i--;
							}
						}
						current_custom_files.AddRanges(custom_soundscape_soundscript_sounds);
					}
				}
				if(extra_files[(int)ExtraFiles.SoundCache] && FindFullFilePath(soundcache, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(soundcache);
					Log(".cache (soundcache) file was detected");
				}
				if(extra_files[(int)ExtraFiles.RetakeBombPlants] && FindFullFilePath(map_retake, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(map_retake);
					Log(".txt (retake bombplants) file was detected");
				}
				if(extra_files[(int)ExtraFiles.CameraPositions] && FindFullFilePath(map_cameras, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(map_cameras);
					Log(".txt (camera positions) file was detected");
				}
				if(extra_files[(int)ExtraFiles.MapStory] && FindFullFilePath(map_story, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(map_story);
					Log(".txt (map story) file was detected");
				}
				if(extra_files[(int)ExtraFiles.ParticlesManifests])
				{
					bool particles_manifest_found = false;
					HashList<string> custom_particles = new HashList<string>(StringComparer.OrdinalIgnoreCase);
					HashSet<string> all_particles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					for(int i = 0;i < particles_manifests.Length;i++)
					{
						string particles_manifest_full_path = FindFullFilePath(particles_manifests[i]).full_file_path; //can be extracted
						if(particles_manifest_full_path != null)
						{
							current_custom_files.Add(particles_manifests[i]);
							Log(".txt (particles manifest \'"+(i + 1)+"\') file was detected");
							particles_manifest_found = true;

							DictionaryList<string, string> pcf_files = GetBlockData(new ContentInfo{file_path = particles_manifest_full_path}, "particles_manifest", "file", parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues;
							if(pcf_files != null)
							{
								for(int k = 0;k < pcf_files[0].Value.Count;k++)
								{
									string pcf_name = pcf_files[0].Value[k];
									pcf_name = PathCombine(pcf_name.StartsWith("!") ? pcf_name.Remove(0, 1) : pcf_name); //already has "particles/"
									if(pcf_name != null && all_particles.Add(pcf_name))
									{
										if(FindFullFilePath(pcf_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_particles.Add(pcf_name);
									}
								}
							}
						}
					}
					if(particles_manifest_found)
					{
						LogFilesInfo(all_particles.Count, custom_particles.Count, "particle{0}");

						for(int i = 0;i < custom_particles.Count;i++)
						{
							FullFilePath particle_info = FindFullFilePath(custom_particles[i]); //can be extracted
							if(particle_info.result == FindFullFilePathResult.Found)
							{
								//read .pcf here
							}
							else
							{
								if(particle_info.result == FindFullFilePathResult.NotFound)Log("Could not find file: "+custom_particles[i], LogType.Warning);
								custom_particles.RemoveAt(i);
								i--;
							}
						}
						current_custom_files.AddRanges(custom_particles);
					}
				}
				string radar_info_full_path = null;
				if(extra_files[(int)ExtraFiles.RadarInformation] && (radar_info_full_path = FindFullFilePath(radar_info, ExtractOption.DontExtract).full_file_path) != null)
				{
					current_custom_files.Add(radar_info);
					Log(".txt (radar informaion) file was detected");
					List<BlockData> vertical_sections_names = GetBlockData(new ContentInfo{file_path = radar_info_full_path}, block_name_pattern: "verticalsections", search_mode: SearchMode.SearchOnlyChildrenBlocks)?[0]?.children_blocks;
					if(vertical_sections_names != null)
					{
						for(int i = 0;i < vertical_sections_names.Count;i++)
						{
							string current_section_name = vertical_sections_names[i].block_name;
							string radar_image = PathCombine("resource/overviews", vmf_name+(current_section_name.EqualsCI("default") ? "" : "_"+vertical_sections_names[i])+"_radar.dds");
							if(FindFullFilePath(radar_image, ExtractOption.DontExtract).full_file_path != null)
							{
								current_custom_files.Add(radar_image);
								Log(".dds (radar image) "+(current_section_name.EqualsCI("default") ? '\'' : '\"')+vertical_sections_names[i]+(current_section_name.EqualsCI("default") ? '\'' : '\"')+" file was detected");
							}
							else
							{
								Log("Could not find file: "+radar_image, LogType.Warning);
							}
						}
					}
					string radar_default_name = PathCombine("resource/overviews", vmf_name+"_radar.dds");
					if(FindFullFilePath(radar_default_name, ExtractOption.DontExtract).full_file_path != null)
					{
						if(current_custom_files.Add(radar_default_name))
						{
							Log(".dds (radar image) 'default' file was detected");
						}
					}
					string radar_spectate_name = PathCombine("resource/overviews", vmf_name+"_radar_spectate.dds");
					if(FindFullFilePath(radar_spectate_name, ExtractOption.DontExtract).full_file_path != null)
					{
						current_custom_files.Add(radar_default_name);
						Log(".dds (radar image) 'spectate' file was detected");
					}
				}
				if(extra_files[(int)ExtraFiles.MapIcon] && FindFullFilePath(map_icon, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(map_icon);
					Log(".svg (map icon) file was detected");
				}
				if(extra_files[(int)ExtraFiles.MapBackground] && FindFullFilePath(map_background, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(map_background);
					Log(".png (map background) file was detected");
				}
				if(extra_files[(int)ExtraFiles.PlayerModels] && FindFullFilePath(kv, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(kv);
					Log(".kv (player models) file was detected");
				}
				if(extra_files[(int)ExtraFiles.DZSpawnMask] && FindFullFilePath(dz_spawn_mask, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(dz_spawn_mask);
					Log(".png (dz spawn mask) file was detected");
				}
				if(extra_files[(int)ExtraFiles.DZDeployemtnMap] && FindFullFilePath(dz_deployment_map, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(dz_deployment_map);
					Log(".png (dz deployment map) file was detected");
				}
				if(extra_files[(int)ExtraFiles.DZTabletMap] && FindFullFilePath(dz_tablet_map, ExtractOption.DontExtract).full_file_path != null)
				{
					current_custom_files.Add(dz_tablet_map);
					Log(".vtf (dz tablet map) file was detected");
				}
			}

			if(custom_files_paths == null)custom_files_paths = new List<string>(current_custom_files.Count);
			for(int i = 0;i < current_custom_files.Count;i++)
			{
				FullFilePath file_info = FindFullFilePath(current_custom_files[i]);
				if(file_info.result == FindFullFilePathResult.Found)
				{
					custom_files_paths.Add(file_info.file_directory);
				}
				else
				{
					if(file_info.result == FindFullFilePathResult.NotFound)Log("Could not find file: "+current_custom_files[i], LogType.Warning);
					current_custom_files.RemoveAt(i);
					i--;
				}
			}

			SetProgress(pack ? 90 : 99);

			if(custom_files == null)custom_files = current_custom_files;
			else custom_files.AddRanges(current_custom_files);

			void PackCustomFilesIntoBSP()
			{
				void WriteCustomFilesList()
				{
					string custom_files_path = PathCombine(cache_path, vmf_name+"_custom_assets.txt");
					File.WriteAllLines(custom_files_path, custom_files.Select((item, index) => PathCombine(custom_files_paths[index], item)));
					Log("Custom assets list saved at: "+custom_files_path);
				}

				if(!Directory.Exists(cache_path))Directory.CreateDirectory(cache_path);
				if(!pack)
				{
					WriteCustomFilesList();
					return;
				}
				if(custom_files.Count == 0)
				{
					Log("0 existing custom assets were found");
					return;
				}
				Log("Packing assets...");
				ConsoleText("Packing assets into the BSP file...");
				if(!File.Exists(input_bsp)) //additional check
				{
					Log("BSP file not found, unable to pack files", LogType.Error);
					//WriteCustomFilesList();
					return;
				}
				if(!IsFileNotLocked(input_bsp, FileAccess.ReadWrite)) //additional check
				{
					Log("BSP file is occupied by another process, unable to pack files", LogType.Error);
					//WriteCustomFilesList();
					return;
				}
				if(do_backup)File.Copy(input_bsp, input_bsp+".backup", true);
				List<ZIP_FilePathInfo> files_list = custom_files.Select((item, index) => new ZIP_FilePathInfo{full_path = PathCombine(custom_files_paths[index], item), relative_path = item}).ToList();
				if(!use_native_tools)
				{
					string files_list_path = PathCombine(cache_path, vmf_name+"_bspzip_files_list.txt");
					File.WriteAllLines(files_list_path, files_list.Select(item => item.relative_path+"\r\n"+item.full_path));
					string bspzip_exe_path = PathCombine(library_path, "steamapps/common", games_info[game].game_folder, "bin", "bspzip.exe");
					string addlist_parameter = "-addlist \""+input_bsp+"\" \""+files_list_path+"\" \""+input_bsp+"\"";
					string game_parameter = "-game \""+(gameinfo_path == null ? PathCombine(library_path, "steamapps/common", games_info[game].game_folder, games_info[game].game_root_folder) : Path.GetDirectoryName(gameinfo_path))+"\"";
					CLAResult bspzip_result = RunCLA(bspzip_exe_path, addlist_parameter+" "+game_parameter);

					void SaveBspzipLog()
					{
						string cmd_log_path = PathCombine(cache_path, "bspzip_log.txt");
						string combined_output = bspzip_result.output_data + bspzip_result.error_data; //\n is already there
						File.WriteAllText(cmd_log_path, combined_output);
						Log("Packing was forced to stop due to "+(bspzip_result.occurred_exception == null ? "bspzip.exe" : "system")+" error (reason: "+(bspzip_result.occurred_exception != null ? bspzip_result.occurred_exception.GetDetailedMessage() : combined_output.ContainsCI("Unable to find gameinfo.txt") ? "unable to find gameinfo.txt" : "unknown")+(bspzip_result.occurred_exception == null ? "; exit code: "+bspzip_result.exit_code : "")+"); bspzip.exe log saved at: "+cmd_log_path, LogType.Error, notification_text_override: "Packing was forced to stop due to "+(bspzip_result.occurred_exception == null ? "bspzip.exe" : "system")+" error");
					}

					if(/*bspzip.occured_exception == null && */bspzip_result.output_data.ContainsCI("Writing new bsp file:"))
					{
						Log("All custom assets were successfully packed");
					}
					else if(bspzip_result.output_data.ContainsCI("usage:") || bspzip_result.error_data.ContainsCI("usage:"))
					{
						bspzip_result = RunCLA(bspzip_exe_path, game_parameter+" "+addlist_parameter); //at least HL2: DM requires this order
						if(/*bspzip.occured_exception == null && */bspzip_result.output_data.ContainsCI("Writing new bsp file:"))
						{
							Log("All custom assets were successfully packed");
						}
						else
						{
							SaveBspzipLog();
						}
					}
					else
					{
						SaveBspzipLog();
					}
					try //try-catch is used to quickly ensure that the file is not being accessed by other processes and there's permission to access it
					{
						File.Delete(files_list_path);
					}
					catch{}
				}
				else
				{
					AddFilesToBSPResult packing_result = AddFilesToBSP(input_bsp, files_list);
					if(packing_result == AddFilesToBSPResult.Success)
					{
						Log("All custom assets were successfully packed");
					}
					else
					{
						Log("Packing was forced to stop due to BSP packing error (reason: "+packing_result.GetTextField()+")", LogType.Error, notification_text_override: "Packing was forced to stop due to BSP packing error");
					}
				}
			}

			if(vmfs_to_pack.Count == 1)
			{
				if(!processing_base_map)Log("", LogType.None);
				PackCustomFilesIntoBSP();
			}

			SetProgress(100);

			vmfs_to_pack.Dequeue();

			if(vmfs_to_pack.Count == 0)
			{
				if(extracted_files.Count > 0)
				{
					if(pack)
					{
						try
						{
							Directory.Delete(extracted_assets_path, true);
						}
						catch{}
					}
					else
					{
						Log("Assets extracted from VPK packages are saved at: "+extracted_assets_path);
					}
				}
				Log("Finished with "+(notification_text == Constants.default_notification ? (warnings_amount+" warning"+(warnings_amount != 1 ? "s" : "")) : "1 error"), print_timestamp: true);
				packing = false;
				vmfs_found = null;
				vmfs_to_pack = null;
				custom_files = null;
				custom_files_paths = null;
				game_vpk_files = null;
				search_paths = null;
				search_vpks = null;
				extracted_files = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				library_path = null;
				warnings_amount = 0;

				/*IntPtr main_form = IntPtr.Zero;
				Invoke(new Action(() => main_form = Handle));
				if(notify)
				{
					if(GetForegroundWindow() != main_form || cmd)
					{
						Notify(" ", notification_text);
					}
				}*/
				if(notify)Notify(" ", notification_text);

				if(!cmd)
				{
					Invoke(new Action(() =>
					{
						button1.Enabled =
						button3.Enabled =
						label1.Enabled =
						label2.Enabled =
						checkBox1.Enabled =
						checkBox2.Enabled =
						linkLabel2.Enabled = true;
						label3.Enabled =
						label4.Enabled =
						button2.Enabled = pack;
						label5.Visible =
						progressBar1.Visible = false;
						label5.Text = "Packing information will be here";
						SetProgress(0);
					}));

					GC.Collect();
					GC.WaitForPendingFinalizers();
				}

				pack_thread = null;
				if(cmd)Invoke(new Action(() => Close()));
			}
			else
			{
				Log("", LogType.None);
				PackData();
			}
		}

		void DetectTexturesInMaterials(ref HashList<string> custom_materials, ref List<List<string>> custom_textures, ref HashSet<string> all_materials, ref HashSet<string> all_textures, ref int missing_custom_materials_amount)
		{
			string ReplaceMaterialVariables(string value, string key, DictionaryList<string, string> vmt_keyvalues)
			{
				Tuple<string, string, Func<bool>>[] keyvalues = new Tuple<string, string, Func<bool>>[]
				{
					new ("levelname", vmf_name, () => vmt_keyvalues.TryGetValue("$levelnamereplacevar", out string level_name_replace_var) && level_name_replace_var.EqualsCI(key))
				};
				for(int i = 0;i < keyvalues.Length;i++)
				{
					if(keyvalues[i].Item3())value = value.Replace("%"+keyvalues[i].Item1+"%", keyvalues[i].Item2, StringComparison.OrdinalIgnoreCase);
				}
				return value;
			}

			for(int i = 0;i < custom_materials.Count;i++)
			{
				FullFilePath vmt_info = FindFullFilePath(custom_materials[i]);
				if(vmt_info.result == FindFullFilePathResult.Found)
				{
					ConsoleText("Reading ("+(i + 1)+"/"+custom_materials.Count+"): "+ShortenFileName(Path.GetFileName(custom_materials[i]), 30));

					custom_textures.Add(new List<string>());
					DictionaryList<string, string> vmt_keyvalues = GetBlockData(new ContentInfo{file_path = vmt_info.full_file_path}, data_search_option: SearchOption.AllDirectories, parent_block_search_option: SearchOption.TopDirectoryOnly)?[0]?.keyvalues;
					if(vmt_keyvalues != null)
					{
						for(int k = 0;k < vmt_keyvalues.Count;k++)
						{
							Match vmt_texture_key = Regex.Match(vmt_keyvalues[k].Key, VMT_texture_keys_pattern, RegexOptions.IgnoreCase);
							if(vmt_texture_key.Success)
							{
								for(int j = 0;j < vmt_keyvalues[k].Value.Count;j++)
								{
									string texture_name_raw = ReplaceMaterialVariables(vmt_keyvalues[k].Value[j], vmt_texture_key.Groups[1].Value, vmt_keyvalues);

									if(texture_name_raw.StartsWithCI("_rt_"))continue;

									KeyParameters key_parameters = GetKeyParameters(Constants.VMT_texture_keys, vmt_keyvalues[k].Key);

									if(key_parameters.default_custom_logic.HasFlag(KeyCustomLogic.IgnoreEnvCubemap) && texture_name_raw.EqualsCI("env_cubemap"))continue;

									string texture_name = VerifyRootFolder(texture_name_raw, "materials", key_parameters.default_root_folder);
									if(texture_name != null)
									{
										if(!key_parameters.default_custom_logic.HasFlag(KeyCustomLogic.PreserveExtension))
										{
											string original_extension = Path.GetExtension(texture_name);
											texture_name = !original_extension.EqualsCI(".hdr") ? Path.ChangeExtension(texture_name, null) : texture_name;
											if(Path.GetExtension(texture_name).EqualsCI(".hdr"))
											{
												string hdr_texture_name = texture_name + ".vtf";
												FindFullFilePathResult file_info_result = FindFullFilePath(hdr_texture_name).result;
												if(file_info_result == FindFullFilePathResult.Found)
												{
													texture_name = hdr_texture_name;
												}
												else if(file_info_result == FindFullFilePathResult.NotFound)
												{
													string non_hdr_texture_name = Path.ChangeExtension(texture_name, ".vtf");
													file_info_result = FindFullFilePath(non_hdr_texture_name).result;
													if(file_info_result == FindFullFilePathResult.Found)
													{
														texture_name = non_hdr_texture_name;
														Log("Could not find file: "+hdr_texture_name+"; switching to: "+non_hdr_texture_name, LogType.Warning);
													}
													else if(file_info_result == FindFullFilePathResult.NotFound)
													{
														texture_name = hdr_texture_name;
													}
													else continue;
												}
												else continue;
											}
											else
											{
												texture_name = VerifyExtension(texture_name + original_extension, ".vtf");
											}
										}
										if(all_textures.Add(texture_name))
										{
											if(FindFullFilePath(texture_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_textures[i].Add(texture_name);
										}
									}
								}
								continue;
							}
							Match vmt_material_key = Regex.Match(vmt_keyvalues[k].Key, VMT_material_keys_pattern, RegexOptions.IgnoreCase);
							if(vmt_material_key.Success)
							{
								for(int j = 0;j < vmt_keyvalues[k].Value.Count;j++)
								{
									KeyParameters key_parameters = GetKeyParameters(Constants.VMT_material_keys, vmt_keyvalues[k].Key);
									string material_name = VerifyRootFolder(ReplaceMaterialVariables(vmt_keyvalues[k].Value[j], vmt_material_key.Groups[1].Value, vmt_keyvalues), "materials", key_parameters.default_root_folder);
									if(material_name != null)
									{
										if(key_parameters.default_custom_logic.HasFlag(KeyCustomLogic.MustHaveExtension))
										{
											if(!Path.GetExtension(material_name).EqualsCI(".vmt"))continue;
										}
										material_name = VerifyExtension(material_name, ".vmt");
										if(all_materials.Add(material_name))
										{
											if(FindFullFilePath(material_name).result != FindFullFilePathResult.FoundInGameVPKs)custom_materials.Add(material_name);
										}
									}
								}
							}
						}
					}
				}
				else
				{
					if(vmt_info.result == FindFullFilePathResult.NotFound)Log("Could not find file: "+custom_materials[i], LogType.Warning);
					custom_materials.RemoveAt(i);
					i--;
					missing_custom_materials_amount++;
				}
			}
		}

		void DetectModelRelatedFiles(HashList<string> models, ref List<List<string>> model_related_files)
		{
			for(int i = 0;i < models.Count;i++)
			{
				FullFilePath mdl_info = FindFullFilePath(models[i]);
				if(mdl_info.result != FindFullFilePathResult.Found)
				{
					model_related_files.Add(null);
					continue;
				}

				string vvd_path = null;
				List<string> all_vtx = new List<string>(); //a model can have more than one .vtx file
				string phy_path = null;
				string ani_path = null;
				if(!mdl_info.file_directory.EqualsCI(extracted_assets_path))
				{
					vvd_path = Path.ChangeExtension(mdl_info.full_file_path, ".vvd");
					Regex regex_file_name = new Regex(@"^"+Regex.Escape(PathCombine(Path.GetFileNameWithoutExtension(models[i])))+@"\.(?:[^.]+\.|)vtx$", RegexOptions.IgnoreCase);
					all_vtx = Directory.EnumerateFiles(Path.GetDirectoryName(mdl_info.full_file_path), "*.vtx", SearchOption.TopDirectoryOnly).Where(item => regex_file_name.IsMatch(Path.GetFileName(item))).Select(item => PathCombine(item)).ToList();
					phy_path = Path.ChangeExtension(mdl_info.full_file_path, ".phy");
					ani_path = Path.ChangeExtension(mdl_info.full_file_path, ".ani");

					if(!File.Exists(vvd_path))vvd_path = null;
					if(!File.Exists(phy_path))phy_path = null;
					if(!File.Exists(ani_path))ani_path = null;
				}
				else
				{
					int vpk_index = extracted_files[/*PathCombine(*/models[i]/*)*/];
					Regex regex_file_name = new Regex(@"^"+Regex.Escape(PathCombine(Path.ChangeExtension(models[i], null)))+@"\.(?:(?:[^.]+\.|)vtx|vvd|phy|ani)$", RegexOptions.IgnoreCase);
					VPKFile[] found_files = search_vpks[vpk_index].AsParallel().Where(item => regex_file_name.IsMatch(item.Key)).Select(item => item.Value).ToArray();
					for(int k = 0;k < found_files.Length;k++)
					{
						string new_path = PathCombine(extracted_assets_path, found_files[k].file_path);
						if(new_path == null)continue;
						if(!extracted_files.ContainsKey(found_files[k].file_path))
						{
							/*if(!use_native_tools) //vpk.exe doesn't seem to be able to extract individual files at all
							{
								//add ", try running without the '"+cmd_options[2]+"' option" below if uncommenting this block
							}
							else
							{*/
								ExtractVPKFileResult extract_vpk_result = ExtractVPKFile(search_paths[vpk_index], found_files[k], out byte[] read_file);
								if(extract_vpk_result == ExtractVPKFileResult.Success)
								{
									Directory.CreateDirectory(Path.GetDirectoryName(new_path));
									File.WriteAllBytes(new_path, read_file);
								}
								else
								{
									Log("Failed to extract \""+found_files[k].file_path+"\" from \""+search_paths[vpk_index]+"\" (reason: "+extract_vpk_result.GetTextField()+")", LogType.Warning);
									continue;
								}
							//}
							extracted_files.Add(found_files[k].file_path, vpk_index);
						}
						string found_file_extension = Path.GetExtension(found_files[k].file_path);
						if(found_file_extension.EqualsCI(".vvd"))
						{
							vvd_path = new_path;
						}
						else if(found_file_extension.EqualsCI(".vtx"))
						{
							all_vtx.Add(new_path);
						}
						else if(found_file_extension.EqualsCI(".phy"))
						{
							phy_path = new_path;
						}
						else/* if(found_file_extension.EqualsCI(".ani"))*/
						{
							ani_path = new_path;
						}
					}
				}
				model_related_files.Add(new List<string>(all_vtx.Count + 3));
				if(vvd_path != null)
				{
					model_related_files[i].Add(Path.ChangeExtension(models[i], ".vvd"));
				}
				for(int k = 0;k < all_vtx.Count;k++)
				{
					model_related_files[i].Add(all_vtx[k].Remove(0, mdl_info.file_directory.Length).TrimStart('/'));
				}
				if(phy_path != null)
				{
					model_related_files[i].Add(Path.ChangeExtension(models[i], ".phy"));
				}
				if(ani_path != null)
				{
					model_related_files[i].Add(Path.ChangeExtension(models[i], ".ani"));
				}
			}
		}

		FullFilePath FindFullFilePath(string path, ExtractOption extract_option = ExtractOption.Extract)
		{
			path = PathCombine(path);

			bool CheckFolder(string folder_path, out FullFilePath output)
			{
				string current_file_path = PathCombine(folder_path, path);
				if(File.Exists(current_file_path))
				{
					output = new FullFilePath{full_file_path = current_file_path, file_directory = folder_path};
					return true;
				}
				output = default;
				return false;
			}

			bool CheckVPK(int vpk_index, out FullFilePath output)
			{
				if(search_vpks[vpk_index].ContainsKey(path))
				{
					string new_path = PathCombine(extracted_assets_path, path);
					if(!extracted_files.ContainsKey(path))
					{
						/*if(!use_native_tools) //vpk.exe doesn't seem to be able to extract individual files at all
						{
							//add ", try running without the '"+cmd_options[2]+"' option" below if uncommenting this block
						}
						else
						{*/
							ExtractVPKFileResult extract_vpk_result = ExtractVPKFile(search_paths[vpk_index], search_vpks[vpk_index][path], out byte[] read_file);
							if(extract_vpk_result == ExtractVPKFileResult.Success)
							{
								Directory.CreateDirectory(Path.GetDirectoryName(new_path));
								File.WriteAllBytes(new_path, read_file);
							}
							else
							{
								Log("Failed to extract \""+path+"\" from \""+search_paths[vpk_index]+"\" (reason: "+extract_vpk_result.GetTextField()+")", LogType.Warning);
								output = new FullFilePath{result = FindFullFilePathResult.FailedToExtract};
								return true;
							}
						//}
						extracted_files.Add(path, vpk_index);
					}
					output = new FullFilePath{full_file_path = new_path, file_directory = extracted_assets_path};
					return true;
				}
				output = default;
				return false;
			}

			for(int i = 0;i < games_info[game].assets_search_order.Count;i++)
			{
				if(games_info[game].assets_search_order[i] == AssetsSearchOrderOption.OwnVPKs)
				{
					foreach(string game_vpk_path in game_vpk_files.Keys)
					{
						if(game_vpk_files[game_vpk_path].Contains(path))return new FullFilePath{result = FindFullFilePathResult.FoundInGameVPKs};
					}
				}
				else if(games_info[game].assets_search_order[i] == AssetsSearchOrderOption.OwnGameFolder)
				{
					if(CheckFolder(PathCombine(library_path, "steamapps/common", games_info[game].game_folder, games_info[game].game_root_folder), out FullFilePath output))return output;
				}
				else/* if(new HashSet<AssetsSearchOrderOption>{AssetsSearchOrderOption.MountedFiles, AssetsSearchOrderOption.MountedVPKs, AssetsSearchOrderOption.MountedFolders}.Contains(games_info[game].assets_search_order[i]))*/
				{
					for(int k = 0;k < search_paths.Count;k++)
					{
						if(Path.GetExtension(search_paths[k]).Length == 0)
						{
							if(games_info[game].assets_search_order[i] != AssetsSearchOrderOption.MountedVPKs && CheckFolder(search_paths[k], out FullFilePath output))return output;
						}
						else
						{
							if(games_info[game].assets_search_order[i] != AssetsSearchOrderOption.MountedFolders)
							{
								if(game_vpk_files.ContainsKey(search_paths[k]))
								{
									if(game_vpk_files[search_paths[k]].Contains(path))return new FullFilePath{result = FindFullFilePathResult.FoundInGameVPKs};
								}
								else
								{
									if(extract_option == ExtractOption.Extract && CheckVPK(k, out FullFilePath output))return output;
								}
							}
						}
					}
				}
			}
			return new FullFilePath{result = FindFullFilePathResult.NotFound};
		}

		string ReadPhyTextData(string phy_path)
		{
			try
			{
				using FileStream file_stream = new FileStream(phy_path, FileMode.Open, FileAccess.Read, FileShare.Read);
				using BinaryReader binary_reader = new BinaryReader(file_stream);

				PhyHeader phy_header = binary_reader.ReadStruct<PhyHeader>();
				for(int i = 0;i < phy_header.solid_count;i++)
				{
					file_stream.Seek(binary_reader.ReadInt32(), SeekOrigin.Current);
				}
				return binary_reader.ReadNullTerminatedString();
			}
			catch
			{
				return null;
			}
		}

		private void Form1_DragEnter(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop) && !packing)
			{
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void Form1_DragDrop(object sender, DragEventArgs e) //allows to drag and drop an entire folder and guarantees a consistent result each time
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if(files.Length == 1 && Directory.Exists(files[0]))
			{
				files = Directory.GetFiles(files[0]);
			}
			files = files.Where(item => new HashSet<string>(StringComparer.OrdinalIgnoreCase){".vmf", ".bsp"}.Contains(Path.GetExtension(item))).GroupBy(item => Path.GetExtension(item), StringComparer.OrdinalIgnoreCase).Select(item => item.OrderBy(item2 => Path.GetFileNameWithoutExtension(item2)).First()).ToArray(); //MinBy is not available in .NET Framework 4.0
			for(int i = 0;i < files.Length;i++)
			{
				if(Path.GetExtension(files[i]).EqualsCI(".vmf"))
				{
					ChangeVMF(files[i]);
				}
				else
				{
					ChangeBSP(files[i]);
				}
			}
		}
	}
}