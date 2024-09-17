using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AutoBSPpackingTool;
using static AutoBSPpackingTool.Util;

namespace VPK
{
	public static class VPK
	{
		private const int VPK_HEADER_SIGNATURE = 0x55aa1234;
		private const int VPK_FILE_STORED_IN_DIR_ARCHIVE = 0x7fff;

		public struct VPKHeader
		{
			//v1, v2
			public uint signature;
			public uint version;
			public uint tree_size;
			//v2
			public uint file_data_section_size;
			public uint archive_md5_section_size;
			public uint other_md5_section_size;
			public uint signature_section_size;
		}

		public struct VPKFile
		{
			public string file_path;
			//public uint crc32; //currently not needed
			public ushort preload_bytes;
			public ushort archive_index;
			public uint entry_offset;
			public uint entry_length;
			public byte[] preload_data;
		}

		public enum ReadVPKResult
		{
			Success,

			[TextField("VPK file is not valid")]
			Exit_NotVPK,

			[TextField("VPK file of this version is not supported")]
			Exit_UnsupportedVersion
		}

		public enum ExtractVPKFileResult
		{
			Success,

			[TextField("numbered VPK file not found")]
			Exit_NumberedVPKNotFound,

			[TextField("failed to read the numbered VPK file")]
			Exit_FailedToReadNumberedVPK,

			[TextField("failed to read the directory VPK file")]
			Exit_FailedToReadDirVPK
		}

		public static ReadVPKResult ReadVPK(string vpk_path, out List<VPKFile> files)
		{
			files = null;
			using FileStream file_stream = new FileStream(vpk_path, FileMode.Open, FileAccess.Read, FileShare.Read);
			using BinaryReader binary_reader = new BinaryReader(file_stream);

			if(file_stream.Length < 12)return ReadVPKResult.Exit_NotVPK;

			//reading a header
			VPKHeader vpk_header = new VPKHeader
			{
				signature = binary_reader.ReadUInt32(),
				version = binary_reader.ReadUInt32(),
				tree_size = binary_reader.ReadUInt32()
			};

			//header and version validation
			if(vpk_header.signature != VPK_HEADER_SIGNATURE)return ReadVPKResult.Exit_NotVPK;
			if(!new HashSet<uint>{1, 2}.Contains(vpk_header.version))return ReadVPKResult.Exit_UnsupportedVersion;

			if(vpk_header.version == 2)
			{
				if(file_stream.Length < 12 + 16)return ReadVPKResult.Exit_NotVPK;
				vpk_header.file_data_section_size = binary_reader.ReadUInt32();
				vpk_header.archive_md5_section_size = binary_reader.ReadUInt32();
				vpk_header.other_md5_section_size = binary_reader.ReadUInt32();
				vpk_header.signature_section_size = binary_reader.ReadUInt32();
			}

			//reading a directory tree
			files = new List<VPKFile>();
			while(true)
			{
				string extension = binary_reader.ReadNullTerminatedString();
				if(extension == "")break;
				while(true)
				{
					string directory = binary_reader.ReadNullTerminatedString();
					if(directory == "")break;
					while(true)
					{
						string filename = binary_reader.ReadNullTerminatedString();
						if(filename == "")break;

						if(file_stream.Length - file_stream.Position < 18)break;
						//uint crc32 = binary_reader.ReadUInt32();
						file_stream.Seek(4, SeekOrigin.Current);
						ushort preload_bytes = binary_reader.ReadUInt16();
						ushort archive_index = binary_reader.ReadUInt16();
						uint entry_offset = binary_reader.ReadUInt32();
						uint entry_length = binary_reader.ReadUInt32();
						//ushort terminator = binary_reader.ReadUInt16();
						file_stream.Seek(2, SeekOrigin.Current);

						if(file_stream.Length - file_stream.Position < preload_bytes)break;
						byte[] preload_data = {};
						if(preload_bytes > 0)
						{
							preload_data = binary_reader.ReadBytes(preload_bytes);
							entry_length += preload_bytes;
						}

						files.Add(new VPKFile
						{
							file_path = !new HashSet<string>{"", " "}.Contains(directory) ? PathCombine(directory, filename+"."+extension) : filename+"."+extension,
							//crc32 = crc32,
							preload_bytes = preload_bytes,
							archive_index = archive_index,
							entry_offset = entry_offset,
							entry_length = entry_length,
							preload_data = preload_data
						});
					}
				}
			}
			return ReadVPKResult.Success;
		}

		private static string GetArchiveIndex(int archive_index)
		{
			return new string('0', Math.Max(3 - archive_index.ToString().Length, 0)) + archive_index;
		}

		public static ExtractVPKFileResult ExtractVPKFile(string vpk_path, VPKFile vpk_file, out byte[] file_data)
		{
			file_data = null;
			byte[] extracted_file = new byte[vpk_file.entry_length];
			if(vpk_file.preload_bytes > 0)
			{
				Array.Copy(vpk_file.preload_data, 0, extracted_file, 0, vpk_file.preload_bytes);
			}
			if(vpk_file.preload_bytes < vpk_file.entry_length)
			{
				if(vpk_file.archive_index != VPK_FILE_STORED_IN_DIR_ARCHIVE) //data is stored in an indexed VPK
				{
					string numbered_vpk_path = PathCombine(Path.GetDirectoryName(vpk_path), Path.GetFileNameWithoutExtension(vpk_path).Remove(Path.GetFileNameWithoutExtension(vpk_path).Length - 3) + GetArchiveIndex(vpk_file.archive_index) + Path.GetExtension(vpk_path));
					if(!File.Exists(numbered_vpk_path))return ExtractVPKFileResult.Exit_NumberedVPKNotFound;
					try
					{
						using(FileStream file_stream = new FileStream(numbered_vpk_path, FileMode.Open, FileAccess.Read, FileShare.Read))
						using(BinaryReader binary_reader = new BinaryReader(file_stream))
						{
							file_stream.Seek(vpk_file.entry_offset, SeekOrigin.Begin);
							binary_reader.Read(extracted_file, vpk_file.preload_bytes, (int)vpk_file.entry_length - vpk_file.preload_bytes);
						}
					}
					catch
					{
						return ExtractVPKFileResult.Exit_FailedToReadNumberedVPK;
					}
				}
				else //data is stored in dir VPK
				{
					try
					{
						using(FileStream file_stream = new FileStream(vpk_path, FileMode.Open, FileAccess.Read, FileShare.Read))
						using(BinaryReader binary_reader = new BinaryReader(file_stream))
						{
							VPKHeader vpk_header = new VPKHeader
							{
								signature = binary_reader.ReadUInt32(),
								version = binary_reader.ReadUInt32(),
								tree_size = binary_reader.ReadUInt32()
							};
							file_stream.Seek(12 + (vpk_header.version == 2 ? 16 : 0) + vpk_header.tree_size + vpk_file.entry_offset, SeekOrigin.Begin);
							binary_reader.Read(extracted_file, vpk_file.preload_bytes, (int)vpk_file.entry_length - vpk_file.preload_bytes);
						}
					}
					catch
					{
						return ExtractVPKFileResult.Exit_FailedToReadDirVPK;
					}
				}
			}
			file_data = extracted_file;
			return ExtractVPKFileResult.Success;
		}
	}
}