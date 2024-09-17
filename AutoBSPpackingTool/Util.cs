using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Collections;
using System.Reflection;
using static BSPZIP.BSPZIP;
using static VPK.VPK;
using static AutoBSPpackingTool.Util;

namespace AutoBSPpackingTool
{
	public static class Util
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct MdlHeader
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
			public string id;
			public int version;
			public int check_sum;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public string name;
			public int data_length;
			public Vector eye_position;
			public Vector illum_position;
			public Vector hull_min;
			public Vector hull_max;
			public Vector view_bb_min;
			public Vector view_bb_max;
			public int flags;
			public int bone_count;
			public int bone_offset;
			public int bone_controller_count;
			public int bone_controller_offset;
			public int hitbox_count;
			public int hitbox_offset;
			public int local_animation_count;
			public int local_animation_offset;
			public int local_sequence_count;
			public int local_sequence_offset;
			public int activity_list_version;
			public int events_indexed;
			public int texture_count;
			public int texture_offset;
			public int texture_dir_count;
			public int texture_dir_offset;
			public int skin_reference_count;
			public int skin_family_count;
			public int skin_family_offset;
			public int body_part_count;
			public int body_part_offset;
			public int attachment_count;
			public int attachment_offset;
			public int local_node_count;
			public int local_node_offset;
			public int local_node_name_offset;
			public int flex_desc_count;
			public int flex_desc_offset;
			public int flex_controller_count;
			public int flex_controller_offset;
			public int flex_rules_count;
			public int flex_rules_offset;
			public int ikchain_count;
			public int ikchain_offset;
			public int mouths_count;
			public int mouths_offset;
			public int local_pose_param_count;
			public int local_pose_param_offset;
			public int surface_prop_offset;
			public int keyvalue_offset;
			public int keyvalue_size;
			public int iklock_count;
			public int iklock_offset;
			public float mass;
			public int contents;
			public int include_models_count;
			public int include_models_offset;
			public int virtual_model;
			public int anim_blocks_name_offset;
			public int anim_blocks_count;
			public int anim_blocks_offset;
			public int anim_block_model;
			public int bone_table_name_offset;
			public int vertex_base;
			public int offset_base;
			public byte directional_light_dot;
			public byte root_lod;
			public byte num_allowed_root_lods;
			public byte unused0;
			public int unused1;
			public int flex_controller_ui_count;
			public int flex_controller_ui_offset;
			public float vert_anim_fixed_point_scale;
			public int unused2;
			public int studio_header2_offset;
			public int unused3;
			//studiohdr2_t
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Vector
		{
			public float X;
			public float Y;
			public float Z;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct PhyHeader
		{
	        public int size;
	        public int id;
	        public int solid_count;
	        public /*long*/int check_sum;
		}

		public struct VtxHeader
		{
			public int version;
			public int vert_cache_size;
			public ushort max_bones_per_strip;
			public ushort max_bones_per_tri;
			public int max_bones_per_vert;
			public int check_sum;
			public int num_lods;
			public int material_replacement_list_offset;
			public int num_body_parts;
			public int body_part_offset;
		}

		public struct GameInfo
		{
			//settings
			public string game_folder;
			public string game_root_folder;
			public List<string> vpk_paths;
			public bool[] available_extra_files;
			public bool[] available_settings;
			public bool[] assets_search_settings;
			public List<AssetsSearchOrderOption> assets_search_order;
			//other
			public long config_priority;
		}

		public struct FullFilePath
		{
			public string full_file_path;
			public string file_directory;
			public FindFullFilePathResult result;
		}

		public struct CLAResult
		{
			public Exception occurred_exception;
			public int exit_code;
			public string output_data;
			public string error_data;
		}

		public struct ContentInfo
		{
			public string file_path;
			public string file_content;
		}

		public class BlockData
		{
			public string block_name;
			public DictionaryList<string, string> keyvalues;
			public List<BlockData> children_blocks;

			public BlockData(string new_block_name, bool is_empty = false)
			{
				block_name = new_block_name;
				if(!is_empty)
				{
					keyvalues = new DictionaryList<string, string>(StringComparer.OrdinalIgnoreCase);
					children_blocks = new List<BlockData>();
				}
			}
		}

		public struct KeyParameters
		{
			public RootFolder default_root_folder;
			public KeyCustomLogic default_custom_logic;
			public Dictionary<string, Tuple<RootFolder, KeyCustomLogic>> classnames_exceptions;
			public Dictionary<string, Tuple<RootFolder, KeyCustomLogic>> extensions_exceptions;

			public KeyParameters(RootFolder default_root_folder, KeyCustomLogic default_custom_logic = default, Dictionary<string, Tuple<RootFolder, KeyCustomLogic>> classnames_exceptions = default, Dictionary<string, Tuple<RootFolder, KeyCustomLogic>> extensions_exceptions = default)
			{
				this.default_root_folder = default_root_folder;
				this.default_custom_logic = default_custom_logic;
				this.classnames_exceptions = classnames_exceptions;
				this.extensions_exceptions = extensions_exceptions;
			}
		}

		public enum ExtraFiles
		{
			[TextField(".nav (navigation mesh)")]
			NavigationMesh = 0,

			[TextField(".ain (info_node)")]
			InfoNode = 1,

			[TextField(".txt (map description)")]
			MapDescription = 2,

			[TextField(".txt (soundscape)")]
			Soundscape = 3,

			[TextField(".txt (soundscript)")]
			SoundScript = 4,

			[TextField(".cache (soundcache)")]
			SoundCache = 5,

			[TextField(".txt (retake bombplants)")]
			RetakeBombPlants = 6,

			[TextField(".txt (camera positions)")]
			CameraPositions = 7,

			[TextField(".txt (map story)")]
			MapStory = 8,

			[TextField(".txt (map commentary)")]
			MapCommentary = 9,

			[TextField(".txt (particles manifests)")]
			ParticlesManifests = 10,

			[TextField(".txt (radar informaion)")]
			RadarInformation = 11,

			[TextField(".svg (map icon)")]
			MapIcon = 12,

			[TextField(".png (map background)")]
			MapBackground = 13,

			[TextField(".kv (player models)")]
			PlayerModels = 14,

			[TextField(".kv3 (bots behaviour)")]
			BotsBehavior = 15,

			[TextField(".png (dz spawn mask)")]
			DZSpawnMask = 16,

			[TextField(".png (dz deployment map)")]
			DZDeployemtnMap = 17,

			[TextField(".vtf (dz tablet map)")]
			DZTabletMap = 18
		}

		public enum Settings
		{
			[TextField("Detect VMF files specified in func_instance entities")]
			DetectVMFsInFuncInstances,

			[TextField("Detect custom files referenced in VScripts and CFGs")]
			DetectFilesInScripts,

			[TextField("Detect search paths (in gameinfo.txt or mount.cfg)")]
			DetectCustomSearchPaths
		}

		public enum UpdateCfgOption
		{
			Ask,
			Update,
			Skip
		}

		public enum ExtractOption
		{
			Extract,
			DontExtract
		}

		public enum SearchOption2
		{
			FirstOccurrence,
			AllOccurrences
		}

		public enum SearchMode
		{
			SearchEverything,
			SearchOnlyKeyvalues,
			SearchOnlyChildrenBlocks
		}

		public enum FindFullFilePathResult
		{
			Found,
			NotFound,
			FailedToExtract,
			FoundInGameVPKs
		}

		public enum FindLibraryDirectoryResult
		{
			Found,
			AppManifestNotFound,
			LibraryFoldersVDFNotFound
		}

		public enum ConfigOption
		{
			Default,
			Actual
		}

		public enum LogType
		{
			[TextField("")]
			None,

			[TextField("[INFO]")]
			Information,

			[TextField("[WARN]")]
			Warning,

			[TextField("[ERROR]")]
			Error,

			[TextField("[FATAL]")]
			Fatal,

			[TextField("[DEBUG]")]
			Debug
		}

		public enum RootFolder
		{
			Absent,
			Present,
			Dynamic
		}

		public enum KeyCustomLogic
		{
			Undefined = 0,
			ProcessAsMaterial = 1 << 0,
			MustHaveExtension = 1 << 1,
			PreserveExtension = 1 << 2,
			IgnoreEnvCubemap = 1 << 3,
			Sprite = 1 << 4
		}
		
		public enum AssetsSearchSettings
		{
			[TextField("Separate mounted folders and VPKs")]
			SeparateMountedFoldersAndVPKs,

			[TextField("Add extra \"Own VPKs\" option")]
			AddExtraOwnVPKs,

			[TextField("Add extra \"Own game folder\" option")]
			AddExtraOwnGameFolder,

			[TextField("Scan for VPKs in mounted folders")]
			ScanForVPKs
		}

		public enum AssetsSearchOrderOption
		{
			[TextField("Own VPKs")]
			OwnVPKs,

			[TextField("Own game folder")]
			OwnGameFolder,

			[TextField("Mounted files")]
			MountedFiles,

			[TextField("Mounted VPKs")]
			MountedVPKs,

			[TextField("Mounted folders")]
			MountedFolders
		}

		public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
		{
			return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
		}

		public static int Round(double value)
		{
			return (int)Math.Round(value, MidpointRounding.AwayFromZero);
		}

		public static double Remap(double value, double input_min, double input_max, double output_min, double output_max)
		{
			return (value - input_min) / (input_max - input_min) * (output_max - output_min) + output_min;
		}

		public static string PathCombine(params string[] paths)
		{
			try
			{
				for(int i = 0;i < paths.Length;i++)
				{
					paths[i] = paths[i].TrimStart('/', '\\');
				}
				string combined_path = Path.Combine(paths);
				StringBuilder fixed_path = new StringBuilder();
				bool slash_met = false;
				for(int i = 0;i < combined_path.Length;i++)
				{
					if(combined_path[i] == '/' || combined_path[i] == '\\')
					{
						if(!slash_met)
						{
							fixed_path.Append('/');
							slash_met = true;
						}
					}
					else
					{
						fixed_path.Append(combined_path[i]);
						slash_met = false;
					}
				}
				return fixed_path.ToString();
			}
			catch
			{
				return null;
			}
		}

		public static string VerifyExtension(string path, string extension)
		{
			try
			{
				if(path == null)return null; //additional check
				return !Path.GetExtension(path).EqualsCI(extension) ? Path.ChangeExtension(path, extension) : path;
			}
			catch
			{
				return null;
			}
		}

		public static string ShortenFileName(string full_file_name, int max_length)
		{
			string file_name = Path.GetFileNameWithoutExtension(full_file_name);
			return file_name.Length > max_length ? file_name.Substring(0, (max_length - 3) / 2) + "..." + file_name.Substring(file_name.Length - (max_length - 3) / 2, (max_length - 3) / 2) + Path.GetExtension(full_file_name) : full_file_name;
		}

		public static KeyParameters GetKeyParameters(Dictionary<string, KeyParameters> dictionary, string found_key)
		{
			string found_key_base = found_key.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			return dictionary.ContainsKey(found_key_base) ? dictionary[found_key_base] : dictionary.ContainsKey(found_key_base+@"\d*") ? dictionary[found_key_base+@"\d*"] : dictionary.ContainsKey(found_key_base+@"\d+") ? dictionary[found_key_base+@"\d+"] : default;
		}

		public static KeyCustomLogic GetVMFKeyCustomLogic(KeyParameters key_parameters, string entity_classname/* = null*/)
		{
			return key_parameters.classnames_exceptions == null || !key_parameters.classnames_exceptions.ContainsKey(entity_classname) ? key_parameters.default_custom_logic : key_parameters.classnames_exceptions[entity_classname].Item2;
		}

		public static string VerifyRootFolder(string path, string root_folder, RootFolder root_folder_option)
		{
			try
			{
				if(path == null)return null; //additional check
				int first_separator = path.IndexOfAny(new char[]{'/', '\\'});
				return root_folder_option == RootFolder.Absent || (root_folder_option == RootFolder.Dynamic && first_separator != -1 && !path.Substring(0, first_separator).EqualsCI(root_folder)) ? PathCombine(root_folder, path) : PathCombine(path);
			}
			catch
			{
				return null;
			}
		}

		public static string VerifyRootFolder(string path, string root_folder, KeyParameters key_parameters, string entity_classname/* = null*/)
		{
			try
			{
				return VerifyRootFolder(path, root_folder, key_parameters.classnames_exceptions == null || !key_parameters.classnames_exceptions.ContainsKey(entity_classname) ? key_parameters.default_root_folder : key_parameters.classnames_exceptions[entity_classname].Item1);
			}
			catch
			{
				return null;
			}
		}

		public static string ReplaceArray<T>(string input, IEnumerable<T> replace, string new_value)
		{
			return Regex.Replace(input, @"(?:"+string.Join(@"|", replace.Select(item => Regex.Escape(item.ToString())))+")", new_value);
		}

		public static bool IsVpkExternal(string vpk_path, string library_path, string game_folder)
		{
			return !PathCombine(vpk_path).StartsWithCI(PathCombine(library_path, "steamapps/common", game_folder)+"/");
		}

		public static bool IsFullPath(string path)
		{
			if(path.Length < 3)return false;
            return Char.IsLetter(path[0]) && path[1] == ':' && new HashSet<char>{'/', '\\'}.Contains(path[2]);
		}

		public static bool IsFileNotLocked(string path, FileAccess file_access)
		{
			try
			{
				using(FileStream file_stream = new FileStream(path, FileMode.Open, file_access, FileShare.None))
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		/*public static string RestorePath(string path)
		{
			bool is_file = File.Exists(path);
			if(!is_file && !Directory.Exists(path))return null;
			path = PathCombine(path)?.TrimEnd('/');
			if(path == null)return null;

			DirectoryInfo directory_info = new DirectoryInfo(path);
			if(is_file)directory_info = directory_info.Parent;

			string current_path = PathCombine(directory_info.Root.FullName);
			current_path = current_path.EndsWith(":/") ? current_path.ToUpper() : current_path;
			List<string> parent_folders = new List<string>();
			while(directory_info != null)
			{
				if(directory_info.Parent == null)break;
				parent_folders.Add(Path.GetFileName(directory_info.FullName));
				directory_info = directory_info.Parent;
			}
			for(int i = 0;i < parent_folders.Count;i++)
			{
				current_path = PathCombine(Directory.EnumerateDirectories(current_path).First(item => Path.GetFileName(item).EqualsCI(parent_folders[parent_folders.Count - 1 - i])));
			}
			if(is_file)
			{
				current_path = PathCombine(Directory.EnumerateFiles(current_path).First(item => Path.GetFileName(item).EqualsCI(Path.GetFileName(path))));
			}
			return current_path;
		}*/

		public static bool IsPermutation<T>(IEnumerable<T> array1, IEnumerable<T> array2)
		{
			Dictionary<T, int> met_times = new Dictionary<T, int>();
			foreach(T item in array1)
			{
				if(!met_times.ContainsKey(item))met_times[item] = 0;
				met_times[item]++;
			}
			foreach(T item in array2)
			{
				if(!met_times.ContainsKey(item) || met_times[item] == 0) return false;
				met_times[item]--;
			}
			return met_times.All(item => item.Value == 0);
		}

		public static Version GetProgramVersion()
		{
			Version full_current_version = Assembly.GetExecutingAssembly().GetName().Version;
			return full_current_version.Revision > 0 ? full_current_version : new Version(full_current_version.Major, full_current_version.Minor, full_current_version.Build);
		}

		public static CLAResult RunCLA(string path, string arguments = "")
		{
			using Process program = new Process();

			program.StartInfo.FileName = path;
			program.StartInfo.Arguments = arguments;
			program.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			program.StartInfo.CreateNoWindow = true;
			program.StartInfo.UseShellExecute = false;
			program.StartInfo.RedirectStandardOutput = true;
			program.StartInfo.RedirectStandardError = true;

			StringBuilder output_builder = new StringBuilder();
			StringBuilder error_builder = new StringBuilder();

			program.OutputDataReceived += (sender, e) =>
			{
				if(e.Data != null)output_builder.AppendLine(e.Data);
			};
			program.ErrorDataReceived += (sender, e) =>
			{
				if(e.Data != null)error_builder.AppendLine(e.Data);
			};

			Exception occurred_exception = null;
			try
			{
				program.Start();

				program.BeginOutputReadLine();
				program.BeginErrorReadLine();

				program.WaitForExit();
			}
			catch(Exception exception)
			{
				occurred_exception = exception;
			}

			return new CLAResult{occurred_exception = occurred_exception, exit_code = program.ExitCode, output_data = output_builder.ToString(), error_data = error_builder.ToString()};
		}

		public static string GetRegistryValue(string key_name, string value_name)
		{
			Dictionary<string, RegistryKey> keys = new Dictionary<string, RegistryKey>
			{
				{"HKEY_CLASSES_ROOT", Registry.ClassesRoot},
				{"HKEY_CURRENT_USER", Registry.CurrentUser},
				{"HKEY_LOCAL_MACHINE", Registry.LocalMachine},
				{"HKEY_USERS", Registry.Users},
				{"HKEY_CURRENT_CONFIG", Registry.CurrentConfig}
			};
			int first_separator = key_name.IndexOfAny(new char[]{'/', '\\'});
			if(first_separator == -1)return null;
			if(!keys.TryGetValue(key_name.Substring(0, first_separator).ToUpper(), out RegistryKey registry_key))return null;
			using(RegistryKey sub_key = registry_key.OpenSubKey(key_name.Remove(0, first_separator + 1), false))
			{
				return sub_key?.GetValue(value_name)?.ToString();
			}
		}

		public static string ReadNullTerminatedString(this BinaryReader binary_reader)
		{
			List<byte> read_bytes = new List<byte>();
			try
			{
				byte next_byte;
				while((next_byte = binary_reader.ReadByte()) != 0)
				{
					read_bytes.Add(next_byte);
				}
			}
			catch(EndOfStreamException){}
			return Encoding.UTF8.GetString(read_bytes.ToArray());
		}

		public static string ReadString(this BinaryReader binary_reader, int length_in_bytes)
		{
			return Encoding.UTF8.GetString(binary_reader.ReadBytes(length_in_bytes));
		}

		public static byte[] StructToBytes<T>(T structure)
		{
			int size = Marshal.SizeOf(structure);
			byte[] array = new byte[size];
			IntPtr ptr = IntPtr.Zero;
			try
			{
				ptr = Marshal.AllocHGlobal(size);
				Marshal.StructureToPtr(structure, ptr, true);
				Marshal.Copy(ptr, array, 0, size);
				return array;
			}
			catch
			{
				return null;
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		public static T BytesToStruct<T>(byte[] bytes)
		{
			int size = Marshal.SizeOf(typeof(T));
			if(bytes.Length < size)
			{
				throw new ArgumentException($"Byte array length ({bytes.Length}) is less than the size of the struct ({size}).");
			}
			IntPtr ptr = IntPtr.Zero;
			try
			{
				ptr = Marshal.AllocHGlobal(size);
				Marshal.Copy(bytes, 0, ptr, size);
				return (T)Marshal.PtrToStructure(ptr, typeof(T));
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		public static T ReadStruct<T>(this BinaryReader binary_reader)
		{
			return BytesToStruct<T>(binary_reader.ReadBytes(Marshal.SizeOf(typeof(T))));
		}

		public static List<BlockData> GetBlockData(ContentInfo input_file, string parent_block_name_pattern = null, string keyvalue_name_pattern = null, string block_name_pattern = null, SearchOption data_search_option = SearchOption.TopDirectoryOnly, SearchOption parent_block_search_option = SearchOption.AllDirectories, SearchOption2 parent_block_search_option2 = SearchOption2.FirstOccurrence, SearchMode search_mode = SearchMode.SearchOnlyKeyvalues, bool start_inside_matched_block = false, bool hierarchize_block_data = false, dynamic replace_keys_with_this_matched_group = null, RegexOptions parent_block_name_options = RegexOptions.IgnoreCase, RegexOptions keyvalue_name_options = RegexOptions.IgnoreCase, RegexOptions block_name_options = RegexOptions.IgnoreCase)
		{
			bool reading_from_file = input_file.file_content == null;
			if(reading_from_file && (input_file.file_path == null || !File.Exists(input_file.file_path)))return null;

			bool use_regex_for_parent_block_name = parent_block_name_pattern != null ? IsRegexPattern(parent_block_name_pattern) : false;
			bool use_regex_for_keyvalue_name = keyvalue_name_pattern != null ? IsRegexPattern(keyvalue_name_pattern) : false;
			bool use_regex_for_block_name = block_name_pattern != null ? IsRegexPattern(block_name_pattern) : false;

			Regex regex_parent_block_name = use_regex_for_parent_block_name ? new Regex(@"^"+parent_block_name_pattern+@"$", parent_block_name_options) : null;
			Regex regex_keyvalue_name = use_regex_for_keyvalue_name ? new Regex(@"^"+keyvalue_name_pattern+@"$", keyvalue_name_options) : null;
			Regex regex_block_name = use_regex_for_block_name ? new Regex(@"^"+block_name_pattern+@"$", block_name_options) : null;

			int i = 0;
			void AddToI(int number = 1)
			{
				i += number;
				UpdateChars();
			}
			StreamReader stream_reader = reading_from_file ? new StreamReader(input_file.file_path) : null;
			int stream_reader_index = i;
			bool reached_end = false;
			int last_char = -1, cur_char = -1, next_char = -1;
			if(reading_from_file)
			{
				cur_char = stream_reader.Read();
				if(cur_char == -1)return null;
				next_char = stream_reader.Read();
			}

			void UpdateChars()
			{
				if(!reading_from_file || reached_end)return;
				while(i > stream_reader_index)
				{
					last_char = cur_char;
					cur_char = next_char;
					next_char = stream_reader.Read();
					stream_reader_index++;
					if(cur_char == -1)
					{
						reached_end = true;
						break;
					}
				}
			}

			char GetCurChar()
			{
				if(reading_from_file)
				{
					return (char)(stream_reader_index == i ? cur_char : stream_reader_index == i + 1 ? last_char : stream_reader_index == i - 1 ? next_char : -1);
				}
				return input_file.file_content[i];
			}

			char GetNextChar()
			{
				if(reading_from_file)
				{
					return (char)(stream_reader_index == i ? next_char : stream_reader_index == i + 1 ? cur_char : -1);
				}
				return input_file.file_content[i + 1];
			}

			bool IsNextCharAvailable()
			{
				return reading_from_file ? next_char != -1 : i < input_file.file_content.Length - 1;
			}

			bool IsEndNotReached()
			{
				return reading_from_file ? !reached_end : i < input_file.file_content.Length;
			}

			StringBuilder current_token_builder = new StringBuilder();
			string ReadToken(char only_separator = '\0')
			{
				current_token_builder.Clear();
				HashSet<char> unquoted_delimiters = new HashSet<char>{'\"', '{', '}'/*, '[', ']'*/};
				for(AddToI(Convert.ToInt32(only_separator != '\0'));IsEndNotReached();AddToI())
				{
					if(only_separator != '\0')
					{
						if(GetCurChar() == only_separator)break;
					}
					else
					{
						if(unquoted_delimiters.Contains(GetCurChar()))
						{
							i--;
							break;
						}
						else if(char.IsWhiteSpace(GetCurChar()))break;
					}
					current_token_builder.Append(GetCurChar());
				}
				return current_token_builder.ToString();
			}

			List<BlockData> found_blocks = !hierarchize_block_data || start_inside_matched_block ? new List<BlockData>{new BlockData(null)} : new List<BlockData>();
			bool first_occurrence_found = start_inside_matched_block; //setting the value to start_inside_matched_block allows to ignore the parent_block_search_option2 parameter if we're already inside a matched block
			bool ReadKeyValues(bool top_directory, bool inside_matched_block, BlockData current_block)
			{
				string last_token = null, current_token = null;
				bool data_found = false;
				for(;IsEndNotReached();AddToI())
				{
					if(char.IsWhiteSpace(GetCurChar()))continue; //skips white spaces and similar
					else if(GetCurChar() == '/' && IsNextCharAvailable() && GetNextChar() == '/') //skips commentaries
					{
						for(AddToI(2);IsEndNotReached();AddToI())
						{
							if(GetCurChar() == '\r')
							{
								if(IsNextCharAvailable() && GetNextChar() == '\n')AddToI();
								break;
							}
							else if(GetCurChar() == '\n')break;
						}
					}
					/*else if(GetCurChar() == '/' && IsNextCharAvailable() && GetNextChar() == '*') //skips multiline commentaries (original parser doesn't support this)
					{
						for(AddToI(2);IsEndNotReached();AddToI())
						{
							if(GetCurChar() == '*' && IsNextCharAvailable() && GetNextChar() == '/')
							{
								AddToI();
								break;
							}
						}
					}*/
					else if(GetCurChar() == '\"') //reads a token in quotes
					{
						current_token = ReadToken('\"');
					}
					else if(GetCurChar() == '{') //reads a nested block
					{
						Match parent_block_name_match = null;
						bool matched = (inside_matched_block && data_search_option == SearchOption.AllDirectories) || (!first_occurrence_found && (top_directory || parent_block_search_option == SearchOption.AllDirectories) && (parent_block_name_pattern == null || (use_regex_for_parent_block_name ? (parent_block_name_match = regex_parent_block_name.Match(last_token ?? "")).Success : (last_token ?? "").Equals(parent_block_name_pattern, parent_block_name_options.HasFlag(RegexOptions.IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))));
						bool changed_first_occurrence_found = false;
						if(parent_block_search_option2 == SearchOption2.FirstOccurrence && !first_occurrence_found && matched)
						{
							first_occurrence_found = true;
							changed_first_occurrence_found = true;
							if(!hierarchize_block_data)found_blocks[0].block_name = replace_keys_with_this_matched_group != null && parent_block_name_match?.Groups[replace_keys_with_this_matched_group].Success == true ? parent_block_name_match.Groups[replace_keys_with_this_matched_group].Value : last_token;
						}
						Match block_name_match = null;
						bool block_passed = search_mode != SearchMode.SearchOnlyKeyvalues && inside_matched_block && (block_name_pattern == null || (use_regex_for_block_name ? (block_name_match = regex_block_name.Match(last_token ?? "")).Success : (last_token ?? "").Equals(block_name_pattern, block_name_options.HasFlag(RegexOptions.IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)));
						if(block_passed)
						{
							data_found = true;
							if(!hierarchize_block_data)found_blocks[0].children_blocks.Add(new BlockData(replace_keys_with_this_matched_group != null && block_name_match?.Groups[replace_keys_with_this_matched_group].Success == true ? block_name_match.Groups[replace_keys_with_this_matched_group].Value : last_token, true));
						}
						BlockData new_block_data = hierarchize_block_data ? new BlockData(last_token) : null;
						AddToI();
						bool current_found_keyvalue = ReadKeyValues(false, matched, new_block_data);
						if(!top_directory)
						{
							data_found = data_found || current_found_keyvalue;
						}
						if(hierarchize_block_data && (block_passed || current_found_keyvalue))
						{
							(current_block?.children_blocks ?? found_blocks).Add(new_block_data);
						}
						if(changed_first_occurrence_found)
						{
							//exits the recursion
							if(reading_from_file)reached_end = true;
							else i = input_file.file_content.Length - 1;
							return data_found;
						}
						last_token = null;
					}
					else if(GetCurChar() == '}') //exits when read a nested block
					{
						if(!top_directory)return data_found;
					}
					else if(GetCurChar() == '[') //reads (skips) a conditional block
					{
						int open_square_brackets = 1;
						for(AddToI();IsEndNotReached();AddToI())
						{
							if(GetCurChar() == '[')open_square_brackets++;
							else if(GetCurChar() == ']')
							{
								open_square_brackets--;
								if(open_square_brackets == 0)break;
							}
						}
					}
					else //reads a token without quotes
					{
						current_token = ReadToken();
					}

					if(current_token != null)
					{
						if(last_token == null)
						{
							last_token = current_token;
						}
						else
						{
							Match keyvalue_name_match = null;
							if(search_mode != SearchMode.SearchOnlyChildrenBlocks && inside_matched_block && (keyvalue_name_pattern == null || (use_regex_for_keyvalue_name ? (keyvalue_name_match = regex_keyvalue_name.Match(last_token)).Success : last_token.Equals(keyvalue_name_pattern, keyvalue_name_options.HasFlag(RegexOptions.IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))))
							{
								(hierarchize_block_data ? current_block : found_blocks[0]).keyvalues.Add(replace_keys_with_this_matched_group != null && keyvalue_name_match?.Groups[replace_keys_with_this_matched_group].Success == true ? keyvalue_name_match.Groups[replace_keys_with_this_matched_group].Value : last_token, current_token);
								data_found = true;
							}
							last_token = null;
						}
						current_token = null;
					}
				}
				return data_found;
			}

			ReadKeyValues(true, start_inside_matched_block, hierarchize_block_data && start_inside_matched_block ? found_blocks[0] : null);
			if(reading_from_file)stream_reader.Dispose();
			return found_blocks;
		}

		public static IEnumerable<string> GetScriptStrings(ContentInfo input_file)
		{
			bool reading_from_file = input_file.file_content == null;
			if(reading_from_file && (input_file.file_path == null || !File.Exists(input_file.file_path)))yield break;

			int i = 0;
			void AddToI(int number = 1)
			{
				i += number;
				UpdateChars();
			}
			StreamReader stream_reader = reading_from_file ? new StreamReader(input_file.file_path) : null;
			int stream_reader_index = i;
			bool reached_end = false;
			int cur_char = -1, next_char = -1;
			if(reading_from_file)
			{
				cur_char = stream_reader.Read();
				if(cur_char == -1)yield break;
				next_char = stream_reader.Read();
			}

			void UpdateChars()
			{
				if(!reading_from_file || reached_end)return;
				while(i > stream_reader_index)
				{
					cur_char = next_char;
					next_char = stream_reader.Read();
					stream_reader_index++;
					if(cur_char == -1)
					{
						reached_end = true;
						break;
					}
				}
			}

			char GetCurChar()
			{
				if(reading_from_file)
				{
					return (char)cur_char;
				}
				return input_file.file_content[i];
			}

			char GetNextChar()
			{
				if(reading_from_file)
				{
					return (char)next_char;
				}
				return input_file.file_content[i + 1];
			}

			bool IsNextCharAvailable()
			{
				return reading_from_file ? next_char != -1 : i < input_file.file_content.Length - 1;
			}

			bool IsEndNotReached()
			{
				return reading_from_file ? !reached_end : i < input_file.file_content.Length;
			}

			Dictionary<char, char> escapable_chars = new Dictionary<char, char>
			{
				{'\'', '\''},
				{'\"', '\"'},
				{'\\', '\\'},
				{'a', '\a'},
				{'b', '\b'},
				{'f', '\f'},
				{'n', '\n'},
				{'r', '\r'},
				{'t', '\t'},
				{'v', '\v'}
			};
			StringBuilder current_string_builder = new StringBuilder();
			string ReadString(bool verbatim = false)
			{
				current_string_builder.Clear();
				for(AddToI(1 + Convert.ToInt32(verbatim));IsEndNotReached();AddToI())
				{
					if(!verbatim)
					{
						if(GetCurChar() == '\\')
						{
							if(GetNextChar() == 'x')
							{
								AddToI(2);
								if(IsNextCharAvailable())
								{
									current_string_builder.Append((char)Convert.ToInt32(string.Concat(GetCurChar(), GetNextChar()), 16));
									AddToI();
								}
							}
							else
							{
								if(escapable_chars.ContainsKey(GetNextChar()))current_string_builder.Append(escapable_chars[GetNextChar()]);
								//else;
								AddToI();
							}
							continue;
						}
					}
					if(GetCurChar() == '\"')
					{
						if(verbatim && IsNextCharAvailable() && GetNextChar() == '\"')AddToI();
						else break;
					}
					current_string_builder.Append(GetCurChar());
				}
				return current_string_builder.ToString();
			}

			HashSet<string> found_strings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for(;IsEndNotReached();AddToI())
			{
				if(char.IsWhiteSpace(GetCurChar()))continue; //skips white spaces and similar
				else if(GetCurChar() == '/' && IsNextCharAvailable() && GetNextChar() == '/') //skips commentaries
				{
					for(AddToI(2);IsEndNotReached();AddToI())
					{
						if(GetCurChar() == '\r')
						{
							if(IsNextCharAvailable() && GetNextChar() == '\n')AddToI();
							break;
						}
						else if(GetCurChar() == '\n')break;
					}
				}
				else if(GetCurChar() == '/' && IsNextCharAvailable() && GetNextChar() == '*') //skips multiline commentaries
				{
					for(AddToI(2);IsEndNotReached();AddToI())
					{
						if(GetCurChar() == '*' && IsNextCharAvailable() && GetNextChar() == '/')
						{
							AddToI();
							break;
						}
					}
				}
				else if(GetCurChar() == '\"') //reads a string
				{
					string current_string = ReadString();
					if(found_strings.Add(current_string))yield return current_string;
				}
				else if(GetCurChar() == '@' && IsNextCharAvailable() && GetNextChar() == '\"') //reads a verbatim string
				{
					string current_string = ReadString(true);
					if(found_strings.Add(current_string))yield return current_string;
				}
			}

			if(reading_from_file)stream_reader.Dispose();
		}

		public static List<List<string>> SplitConsoleCommand(ContentInfo input_file)
		{
			bool reading_from_file = input_file.file_content == null;
			if(reading_from_file && (input_file.file_path == null || !File.Exists(input_file.file_path)))return null;

			int i = 0;
			void AddToI(int number = 1)
			{
				i += number;
				UpdateChars();
			}
			StreamReader stream_reader = reading_from_file ? new StreamReader(input_file.file_path) : null;
			int stream_reader_index = i;
			bool reached_end = false;
			int last_char = -1, cur_char = -1, next_char = -1;
			if(reading_from_file)
			{
				cur_char = stream_reader.Read();
				if(cur_char == -1)return null;
				next_char = stream_reader.Read();
			}

			void UpdateChars()
			{
				if(!reading_from_file || reached_end)return;
				while(i > stream_reader_index)
				{
					last_char = cur_char;
					cur_char = next_char;
					next_char = stream_reader.Read();
					stream_reader_index++;
					if(cur_char == -1)
					{
						reached_end = true;
						break;
					}
				}
			}

			char GetCurChar()
			{
				if(reading_from_file)
				{
					return (char)(stream_reader_index == i ? cur_char : stream_reader_index - i == 1 ? last_char : stream_reader_index - i == -1 ? next_char : -1);
				}
				return input_file.file_content[i];
			}

			char GetNextChar()
			{
				if(reading_from_file)
				{
					return (char)(stream_reader_index == i ? next_char : stream_reader_index - i == 1 ? cur_char : -1);
				}
				return input_file.file_content[i + 1];
			}

			bool IsNextCharAvailable()
			{
				return reading_from_file ? next_char != -1 : i < input_file.file_content.Length - 1;
			}

			bool IsEndNotReached()
			{
				return reading_from_file ? !reached_end : i < input_file.file_content.Length;
			}

			bool CheckIfNewLine()
			{
				if(GetCurChar() == '\r' || GetCurChar() == '\n')
				{
					if(GetCurChar() == '\r')
					{
						if(IsNextCharAvailable() && GetNextChar() == '\n')AddToI();
					}
					return true;
				}
				return false;
			}

			HashSet<char> unquoted_delimiters = new HashSet<char>{'\"', ';'};
			StringBuilder current_string_builder = new StringBuilder();
			string ReadToken(bool quoted = false, bool is_script = false)
			{
				current_string_builder.Clear();
				if(is_script)
				{
					bool in_quote = quoted;
					for(AddToI(Convert.ToInt32(quoted));IsEndNotReached();AddToI())
					{
						if(GetCurChar() == '\"')in_quote = !in_quote; //console command parser doesn't recognize escaped quotes
						else if((!in_quote && GetCurChar() == ';') || CheckIfNewLine())
						{
							i--;
							break;
						}
						current_string_builder.Append(GetCurChar());
					}
					return current_string_builder.ToString();
				}
				for(AddToI(Convert.ToInt32(quoted));IsEndNotReached();AddToI())
				{
					if(quoted)
					{
						if(GetCurChar() == '\"')break;
						else if(CheckIfNewLine())
						{
							i--;
							break;
						}
					}
					else
					{
						if(unquoted_delimiters.Contains(GetCurChar()) || CheckIfNewLine())
						{
							i--;
							break;
						}
						else if(char.IsWhiteSpace(GetCurChar()))break;
					}
					current_string_builder.Append(GetCurChar());
				}
				return current_string_builder.ToString();
			}

			List<List<string>> console_commands = new List<List<string>>();
			int current_index = 0;
			string current_token = null;
			bool is_script = false;
			for(;IsEndNotReached();AddToI())
			{
				if(CheckIfNewLine() || GetCurChar() == ';')
				{
					current_index++;
					is_script = false;
				}
				else if(char.IsWhiteSpace(GetCurChar()))continue; //skips white spaces and similar
				else if(GetCurChar() == '/' && IsNextCharAvailable() && GetNextChar() == '/') //skips commentaries
				{
					for(AddToI(2);IsEndNotReached();AddToI())
					{
						if(GetCurChar() == '\r')
						{
							if(IsNextCharAvailable() && GetNextChar() == '\n')AddToI();
							break;
						}
						else if(GetCurChar() == '\n')break;
					}
				}
				/*else if(GetCurChar() == '/' && IsNextCharAvailable() && GetNextChar() == '*') //skips multiline commentaries
				{
					for(AddToI(2);IsEndNotReached();AddToI())
					{
						if(GetCurChar() == '*' && IsNextCharAvailable() && GetNextChar() == '/')
						{
							AddToI();
							break;
						}
					}
				*/
				else if(GetCurChar() == '\"') //reads a token in quotes
				{
					current_token = ReadToken(quoted: true, is_script: is_script);
				}
				else //reads a token without quotes
				{
					current_token = ReadToken(is_script: is_script);
				}

				if(current_token != null)
				{
					if(current_index >= console_commands.Count)
					{
						console_commands.Add(new List<string>{current_token});
						current_index = console_commands.Count - 1;
						if(current_token.EqualsCI("script"))is_script = true;
					}
					else
					{
						console_commands[current_index].Add(current_token);
					}
					current_token = null;
				}
			}

			if(reading_from_file)stream_reader.Dispose();
			return console_commands;
		}

		//this is actually faster than using the original function
		public static IEnumerable<T> Except<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer = null)
		{
			HashSet<T> hashset = new HashSet<T>(second, comparer);
			return first.Where(item => !hashset.Contains(item));
		}
		public static IEnumerable<T> Except<T>(this IEnumerable<T> first, HashSet<T> second)
		{
			return first.Where(item => !second.Contains(item));
		}
		public static IEnumerable<T> Except<T>(this IEnumerable<T> first, HashList<T> second)
		{
			return first.Where(item => !second.Contains(item));
		}

		/*public static HashList<T> ToHashList<T>(this IEnumerable<T> source)
		{
			return new HashList<T>(source);
		}*/

		public static bool BoolAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if(!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, value);
				return true;
			}
			return false;
		}

		public static bool EqualsCI(this string input, string value)
		{
			return input.Equals(value, StringComparison.OrdinalIgnoreCase);
		}

		public static bool ContainsCI(this string input, string value)
		{
			return input.IndexOfCI(value) != -1;
		}

		public static int IndexOfCI(this string input, string value, int start_index = 0)
		{
			return input.IndexOf(value, start_index, StringComparison.OrdinalIgnoreCase);
		}

		public static int IndexOfCI(this string input, string value, int start_index, int count)
		{
			return input.IndexOf(value, start_index, count, StringComparison.OrdinalIgnoreCase);
		}

		public static bool StartsWithCI(this string input, string value)
		{
			return input.StartsWith(value, StringComparison.OrdinalIgnoreCase);
		}

		public static bool EndsWithCI(this string input, string value)
		{
			return input.EndsWith(value, StringComparison.OrdinalIgnoreCase);
		}

		public static string Replace(this string input, string old_value, string new_value, StringComparison comparison_type)
		{
			StringBuilder output_builder = new StringBuilder();
			int start_index = 0;
			int occurrence_index;
			while((occurrence_index = input.IndexOf(old_value, start_index, comparison_type)) != -1)
			{
				output_builder.Append(input, start_index, occurrence_index - start_index);
				output_builder.Append(new_value);
				start_index = occurrence_index + old_value.Length;
			}
			output_builder.Append(input, start_index, input.Length - start_index);
			return output_builder.ToString();
		}

		public static bool IsRegexEscapable(this char c)
		{
			return new HashSet<char>{'\\', '*', '+', '?', '|', '{', '[', '(', ')', '^', '$', '.', '#'}.Contains(c);
		}

		public static bool IsRegexPattern(string input, int start_index = 0, int count = -1)
		{
			if(count == -1)count = input.Length - start_index;
			bool escape_next = false;
			for(int i = start_index;i < start_index + count;i++)
			{
				if(escape_next)
				{
					if(new HashSet<char>{'A', 'b', 'B', 'd', 'D', 'f', 'G', 'k', 'n', 'r', 's', 'S', 't', 'u', 'v', 'w', 'W', 'x', 'Z'}.Contains(input[i]))return true;
					escape_next = false;
					continue;
				}
				if(input[i] == '\\')
				{
					escape_next = true;
					continue;
				}

				if(input[i].IsRegexEscapable())return true;
			}
			return false;
		}

		public static string GetDetailedMessage(this Exception exception)
		{
			string detailed_message = exception.GetType().FullName+": "+exception.Message;
			return detailed_message;
		}

		public static string GetTextField<TEnum>(this TEnum enum_value) where TEnum : Enum
		{
			return ((TextFieldAttribute)Attribute.GetCustomAttribute(enum_value.GetType().GetField(enum_value.ToString()), typeof(TextFieldAttribute)))?.text ?? "missing text field";
		}

		public static long GetUnixTimeMilliseconds() //DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() is not available in .NET Framework 4.0
		{
			return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class TextFieldAttribute : Attribute
	{
		public string text{get;}

		public TextFieldAttribute(string text)
		{
			this.text = text;
		}
	}

	public class GameInfoPriorityComparer : IComparer<GameInfo> //IComparer<GameInfo>.Create() is not available in .NET Framework 4.0
	{
		public int Compare(GameInfo a, GameInfo b)
		{
			return a.config_priority.CompareTo(b.config_priority);
		}
	}

	class Crc32
	{
		private static readonly uint[] crc32_table;

		static Crc32()
		{
			const uint polynomial = 0xEDB88320u;
			crc32_table = new uint[256];
			for(uint i = 0;i < 256;i++)
			{
				uint crc = i;
				for(uint j = 8;j > 0;j--)
				{
					crc = (crc >> 1) ^ ((crc & 1) == 1 ? polynomial : 0);
				}
				crc32_table[i] = crc;
			}
		}

		public static uint ComputeChecksum(byte[] bytes)
		{
			uint crc = 0xFFFFFFFFu;
			foreach(byte b in bytes)
			{
				crc = (crc >> 8) ^ crc32_table[(crc ^ b) & 0xFF];
			}
			return ~crc;
		}

		public static string ComputeChecksumString(byte[] bytes)
		{
			return ComputeChecksum(bytes).ToString("X8");
		}
	}

	public class HashList<T> : ICollection<T>
	{
		private readonly HashSet<T> _hashset;
		private readonly List<T> _list;

		public HashList()
		{
			_hashset = new HashSet<T>();
			_list = new List<T>();
		}

		public HashList(int capacity)
		{
			_hashset = new HashSet<T>();
			_list = new List<T>(capacity);
		}

		public HashList(IEqualityComparer<T> comparer)
		{
			_hashset = new HashSet<T>(comparer);
			_list = new List<T>();
		}

		public HashList(params IEnumerable<T>[] collections)
		{
			_hashset = new HashSet<T>();
			_list = new List<T>();
			AddRanges(collections);
		}

		public HashList(int capacity, IEqualityComparer<T> comparer)
		{
			_hashset = new HashSet<T>(comparer);
			_list = new List<T>(capacity);
		}

		public int Count => _list.Count;
		public bool IsReadOnly => false;

		void ICollection<T>.Add(T item)
		{
			Add(item);
		}

		public bool Add(T item) //O(1)
		{
			if(_hashset.Add(item))
			{
				_list.Add(item);
				return true;
			}
			return false;
		}

		public void AddRanges(params IEnumerable<T>[] collections)
		{
			foreach(IEnumerable<T> collection in collections)
			{
				if(collection == null)continue;
				_list.AddRange(collection.Where(item => _hashset.Add(item)));
			}
		}

		public bool Contains(T item) //O(1)
		{
			return _hashset.Contains(item);
		}

		public bool Remove(T item) //O(n)
		{
			if(_hashset.Remove(item))
			{
				_list.RemoveAt(IndexOf(item));
				return true;
			}
			return false;
		}

		public void RemoveAt(int index) //O(n)
		{
			_hashset.Remove(_list[index]);
			_list.RemoveAt(index);
		}

		public int IndexOf(T item) //O(n)
		{
			return _list.FindIndex(item2 => _hashset.Comparer.Equals(item2, item));
		}

		public void Insert(int index, T item) //O(n)
		{
			if(_hashset.Add(item))
			{
				_list.Insert(index, item);
			}
		}

		public void InsertRange(int index, IEnumerable<T> collection)
		{
			_list.InsertRange(index, collection.Where(item => _hashset.Add(item)));
		}

		public void Clear()
		{
			_hashset.Clear();
			_list.Clear();
		}

		public void CopyTo(T[] array, int array_index)
		{
			_list.CopyTo(array, array_index);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public T this[int index] => _list[index]; //O(1)
	}

	public class DictionaryList<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<TKey>, IEnumerable<KeyValuePair<TKey, List<TValue>>>
	{
		private readonly Dictionary<TKey, List<TValue>> _dictionary;
		private readonly List<TKey> _list;
		private readonly List<TKey> _list_total;

		public DictionaryList()
		{
			_dictionary = new Dictionary<TKey, List<TValue>>();
			_list = new List<TKey>();
			_list_total = new List<TKey>();
		}

		public DictionaryList(int capacity)
		{
			_dictionary = new Dictionary<TKey, List<TValue>>(capacity);
			_list = new List<TKey>(capacity);
			_list_total = new List<TKey>(capacity);
		}

		public DictionaryList(IEqualityComparer<TKey> comparer)
		{
			_dictionary = new Dictionary<TKey, List<TValue>>(comparer);
			_list = new List<TKey>();
			_list_total = new List<TKey>();
		}

		public DictionaryList(params IEnumerable<KeyValuePair<TKey, List<TValue>>>[] collections)
		{
			_dictionary = new Dictionary<TKey, List<TValue>>();
			_list = new List<TKey>();
			_list_total = new List<TKey>();
			AddRanges(collections);
		}

		public int Count => _list.Count;
		public int CountTotal => _list_total.Count;
		public bool IsReadOnly => false;

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item);
		}

		public bool Add(KeyValuePair<TKey, TValue> item) //O(1)
		{
			return Add(item.Key, item.Value);
		}

		public bool Add(TKey key, TValue value) //O(1)
		{
			if(!_dictionary.ContainsKey(key))
			{
				_dictionary.Add(key, new List<TValue>{value});
				_list.Add(key);
			}
			else
			{
				_dictionary[key].Add(value);
			}
			_list_total.Add(key);
			return true;
		}

		public bool Add(KeyValuePair<TKey, List<TValue>> item) //O(n)
		{
			return Add(item.Key, item.Value);
		}

		public bool Add(TKey key, List<TValue> value) //O(n)
		{
			if(!_dictionary.ContainsKey(key))
			{
				_dictionary.Add(key, value);
				_list.Add(key);
			}
			else
			{
				_dictionary[key].AddRange(value);
			}
			for(int i = 0;i < value.Count;i++)
			{
				_list_total.Add(key);
			}
			return true;
		}

		public void AddRanges(params IEnumerable<KeyValuePair<TKey, TValue>>[] collections)
		{
			foreach(IEnumerable<KeyValuePair<TKey, TValue>> collection in collections)
			{
				if(collection == null)continue;
				foreach(KeyValuePair<TKey, TValue> item in collection)
				{
					Add(item);
				}
			}
		}

		public void AddRanges(params IEnumerable<KeyValuePair<TKey, List<TValue>>>[] collections)
		{
			foreach(IEnumerable<KeyValuePair<TKey, List<TValue>>> collection in collections)
			{
				if(collection == null)continue;
				foreach(KeyValuePair<TKey, List<TValue>> item in collection)
				{
					Add(item);
				}
			}
		}

		public bool Contains(KeyValuePair<TKey, TValue> item) //O(n)
		{
			return _dictionary.ContainsKey(item.Key) && _dictionary[item.Key].Contains(item.Value);
		}

		public bool ContainsKey(TKey key) //O(1)
		{
			return _dictionary.ContainsKey(key);
		}

		public bool TryGetValues(TKey key, out List<TValue> value) //O(1)
		{
			if(_dictionary.ContainsKey(key))
			{
				value = _dictionary[key];
				return true;
			}
			value = default;
			return false;
		}

		public bool TryGetValue(TKey key, out TValue value, int index = 0) //O(1)
		{
			if(_dictionary.ContainsKey(key))
			{
				value = _dictionary[key][index];
				return true;
			}
			value = default;
			return false;
		}

		public bool Remove(KeyValuePair<TKey, TValue> item) //O(n)
		{
			if(_dictionary.ContainsKey(item.Key))
			{
				if(_dictionary[item.Key].Count == 1 && _dictionary[item.Key][0].Equals(item.Value))
				{
					_dictionary.Remove(item.Key);
					_list.Remove(item.Key);
					_list_total.Remove(item.Key);
				}
				else
				{
					int index_of = _dictionary[item.Key].IndexOf(item.Value);

					if(index_of == -1)return false;

					_dictionary[item.Key].RemoveAt(index_of);
					for(int i = 0;i < CountTotal;i++)
					{
						if(_list_total[i].Equals(item.Key))
						{
							if(index_of-- == 0)
							{
								_list_total.RemoveAt(i);
								break;
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		public bool Remove(TKey key) //O(n)
		{
			if(_dictionary.Remove(key))
			{
				_list.Remove(key);
				_list_total.RemoveAll(item => item.Equals(key));
				return true;
			}
			return false;
		}

		public void RemoveAt(int index) //O(n)
		{
			if(index < 0 || index >= _list.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			Remove(_list[index]);
		}

		public void Clear()
		{
			_dictionary.Clear();
			_list.Clear();
			_list_total.Clear();
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int array_index)
		{
			if(array == null)throw new ArgumentNullException(nameof(array));
			if(array_index < 0 || array_index >= array.Length)throw new ArgumentOutOfRangeException(nameof(array_index));
			if(array.Length - array_index < CountTotal)throw new ArgumentException();

			foreach(KeyValuePair<TKey, TValue> item in this)
			{
				array[array_index++] = item;
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			Dictionary<TKey, int> met_times = new Dictionary<TKey, int>();
			foreach(TKey key in _list_total)
			{
				if(!met_times.ContainsKey(key))met_times[key] = 0;
				yield return new KeyValuePair<TKey, TValue>(key, _dictionary[key][met_times[key]++]);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			return _list.GetEnumerator();
		}

		IEnumerator<KeyValuePair<TKey, List<TValue>>> IEnumerable<KeyValuePair<TKey, List<TValue>>>.GetEnumerator()
		{
			foreach(TKey key in _list)
			{
				yield return new KeyValuePair<TKey, List<TValue>>(key, _dictionary[key]);
			}
		}

		public KeyValuePair<TKey, List<TValue>> this[int index] => new (_list[index], _dictionary[_list[index]]); //O(1)
		public List<TValue> this[TKey key] => _dictionary[key]; //O(1)
	}
}