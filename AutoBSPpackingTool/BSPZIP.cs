using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AutoBSPpackingTool;
using static AutoBSPpackingTool.Util;

namespace BSPZIP
{
	public static class BSPZIP
	{
		private const int HEADER_LUMPS = 64;
		private const int IDBSPHEADER = ('P'<<24) | ('S'<<16) | ('B'<<8) | 'V';

		public struct BspHeader
		{
			public int identifier;
			public int version;
			public Lump[] lumps;
			public int map_revision;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Lump
		{
			public int file_offset;
			public int file_length;
			public int version;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] fourCC;
		}

		public struct ZIP_FilePathInfo
		{
			public string full_path;
			public string relative_path;
		}

		public struct ZIP_File
		{
			public ZIP_FileHeader file_header;
			public byte[] data;
			public string file_name;
			public byte[] extra_field;
			public string file_comment;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ZIP_EndOfCentralDirRecord
		{
			public uint signature;
			public ushort number_of_this_disk;
			public ushort number_of_the_disk_with_start_of_central_dir;
			public ushort central_dir_entries_this_disk;
			public ushort central_dir_entries_total;
			public uint central_dir_size;
			public uint start_of_central_dir_offset;
			public ushort comment_length;
			//zip file comment

			public ZIP_EndOfCentralDirRecord(int central_dir_entries, int central_dir_size, int start_of_central_dir_offset, string comment = "")
			{
				this.signature = PKID(5, 6);
				this.number_of_this_disk = 0;
				this.number_of_the_disk_with_start_of_central_dir = 0;
				this.central_dir_entries_this_disk = (ushort)central_dir_entries;
				this.central_dir_entries_total = (ushort)central_dir_entries;
				this.central_dir_size = (uint)central_dir_size;
				this.start_of_central_dir_offset = (uint)start_of_central_dir_offset;
				this.comment_length = (ushort)comment.Length;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ZIP_FileHeader
		{
			public uint signature;
			public ushort version_made_by;
			public ushort version_needed_to_extract;
			public ushort flags;
			public ushort compression_method;
			public ushort last_modified_time;
			public ushort last_modified_date;
			public uint crc32;
			public uint compressed_size;
			public uint uncompressed_size;
			public ushort file_name_length;
			public ushort extra_field_length;
			public ushort file_comment_length;
			public ushort disk_number_start;
			public ushort internal_file_attribs;
			public uint external_file_attribs;
			public uint relative_offset_of_local_header;
			//file name
			//extra field
			//file comment

			public ZIP_FileHeader(string file_name, uint crc32, uint compressed_size, uint uncompressed_size, uint relative_offset_of_local_header, CompressionType compression_method = CompressionType.None)
			{
				this.signature = PKID(1, 2);
				this.version_made_by = 10;
				this.version_needed_to_extract = 10;
				this.flags = 0;
				this.compression_method = (ushort)compression_method;
				this.last_modified_time = 0;
				this.last_modified_date = 0;
				this.crc32 = crc32;
				this.compressed_size = compressed_size;
				this.uncompressed_size = uncompressed_size;
				this.file_name_length = (ushort)file_name.Length;
				this.extra_field_length = 0;
				this.file_comment_length = 0;
				this.disk_number_start = 0;
				this.internal_file_attribs = 0;
				this.external_file_attribs = 0;
				this.relative_offset_of_local_header = relative_offset_of_local_header;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ZIP_LocalFileHeader
		{
			public uint signature;
			public ushort version_needed_to_extract;
			public ushort flags;
			public ushort compression_method;
			public ushort last_modified_time;
			public ushort last_modified_date;
			public uint crc32;
			public uint compressed_size;
			public uint uncompressed_size;
			public ushort file_name_length;
			public ushort extra_field_length;
			//file name
	        //extra field

			public ZIP_LocalFileHeader(string file_name, uint crc32, uint compressed_size, uint uncompressed_size, CompressionType compression_method = CompressionType.None)
			{
				this.signature = PKID(3, 4);
				this.version_needed_to_extract = 10;
				this.flags = 0;
				this.compression_method = (ushort)compression_method;
				this.last_modified_time = 0;
				this.last_modified_date = 0;
				this.crc32 = crc32;
				this.compressed_size = compressed_size;
				this.uncompressed_size = uncompressed_size;
				this.file_name_length = (ushort)file_name.Length;
				this.extra_field_length = 0;
			}
		}

		public enum CompressionType
	    {
		    Unknown = -1,
		    None = 0,
		    LZMA = 14
	    }

		public enum AddFilesToBSPResult
		{
			Success,

			[TextField("BSP file is not valid")]
			Exit_NotBSP,

			[TextField("BSP's pak lump is corrupted")]
			Exit_CorruptedPakLump,

			[TextField("failed to read the central directory of the pak lump")]
			Exit_FailedToReadCentralDirectory,

			[TextField("failed to read the files' data inside the pak lump")]
			Exit_FailedToReadFilesData
		}

		public static uint PKID(int a, int b)
		{
			return (uint)((b << 24) | (a << 16) | ('K' << 8) | 'P');
		}

		public static AddFilesToBSPResult AddFilesToBSP(string bsp_path, List<ZIP_FilePathInfo> files)
		{
			for(int i = 0;i < files.Count;i++)
			{
				ZIP_FilePathInfo current_file_info = files[i];
				current_file_info.relative_path = PathCombine(current_file_info.relative_path);
				files[i] = current_file_info;
			}

			using FileStream file_stream = new FileStream(bsp_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			using BinaryReader binary_reader = new BinaryReader(file_stream);

			if(file_stream.Length < 12 + 16 * HEADER_LUMPS)return AddFilesToBSPResult.Exit_NotBSP;

			//reading a header
			BspHeader bsp_header = new BspHeader
			{
				identifier = binary_reader.ReadInt32(),
				version = binary_reader.ReadInt32(),
				lumps = new Lump[HEADER_LUMPS]
			};
			long pak_lump_length_offset = 0;
			for(int i = 0;i < HEADER_LUMPS;i++)
			{
				if(i == 40)pak_lump_length_offset = file_stream.Position + 4;
				bsp_header.lumps[i] = binary_reader.ReadStruct<Lump>();
			}
			bsp_header.map_revision = binary_reader.ReadInt32();

			//header validation
			if(bsp_header.identifier != IDBSPHEADER)return AddFilesToBSPResult.Exit_NotBSP;

			//finding an end record
			ZIP_EndOfCentralDirRecord end_record = new ZIP_EndOfCentralDirRecord{signature = 0};
			string end_record_comment = "";
			{
				int start_offset = bsp_header.lumps[40].file_length - Marshal.SizeOf(typeof(ZIP_EndOfCentralDirRecord));
				if(start_offset < 0)
				{
					return AddFilesToBSPResult.Exit_CorruptedPakLump; //pak lump is corrupted or not present for some reason
				}
				for(int offset = start_offset;offset >= Math.Max(start_offset - ushort.MaxValue, 0);offset--)
				{
					file_stream.Seek(bsp_header.lumps[40].file_offset + offset, SeekOrigin.Begin);
					uint signature = binary_reader.ReadUInt32();
					if(signature == PKID(5, 6))
					{
						file_stream.Seek(-4, SeekOrigin.Current);
						end_record = binary_reader.ReadStruct<ZIP_EndOfCentralDirRecord>();
						end_record_comment = binary_reader.ReadString(end_record.comment_length);
						break;
					}
				}
				if(end_record.signature == 0)return AddFilesToBSPResult.Exit_CorruptedPakLump; //pak lump is corrupted or has no end record for some reason
			}

			//reading a central directory
			HashSet<string> new_files = new HashSet<string>(files.Select(item => item.relative_path), StringComparer.OrdinalIgnoreCase);
			List<ZIP_File> zip_files = new List<ZIP_File>(end_record.central_dir_entries_total);
			try
			{
				file_stream.Seek(bsp_header.lumps[40].file_offset + end_record.start_of_central_dir_offset, SeekOrigin.Begin);
				for(int i = 0;i < end_record.central_dir_entries_total;i++)
				{
					ZIP_FileHeader current_file_header = binary_reader.ReadStruct<ZIP_FileHeader>();
					string current_file_path = PathCombine(binary_reader.ReadString(current_file_header.file_name_length)); //PathCombine is used here just in case
					byte[] current_extra_field = binary_reader.ReadBytes(current_file_header.extra_field_length);
					string current_file_comment = binary_reader.ReadString(current_file_header.file_comment_length);
					if(new_files.Contains(current_file_path))continue; //this also "removes" original extra field and file comment if they're present
					zip_files.Add(new ZIP_File
					{
						file_header = current_file_header,
						file_name = current_file_path,
						extra_field = current_extra_field,
						file_comment = current_file_comment
					});
				}
			}
			catch
			{
				return AddFilesToBSPResult.Exit_FailedToReadCentralDirectory;
			}

			//reading files data
			try
			{
				for(int i = 0;i < zip_files.Count;i++)
				{
					file_stream.Seek(bsp_header.lumps[40].file_offset + zip_files[i].file_header.relative_offset_of_local_header, SeekOrigin.Begin);
					ZIP_LocalFileHeader local_header = binary_reader.ReadStruct<ZIP_LocalFileHeader>();
					file_stream.Seek(local_header.file_name_length + local_header.extra_field_length, SeekOrigin.Current);
					ZIP_File current_zip_file = zip_files[i];
					current_zip_file.data = binary_reader.ReadBytes((int)local_header.compressed_size);
					zip_files[i] = current_zip_file;
				}
			}
			catch
			{
				return AddFilesToBSPResult.Exit_FailedToReadFilesData;
			}

			//writing a new pak lump
			using(BinaryWriter binary_writer = new BinaryWriter(file_stream))
			{
				file_stream.Seek(bsp_header.lumps[40].file_offset, SeekOrigin.Begin);
				WriteNewPakLump(zip_files, files, end_record, end_record_comment, binary_writer);
				file_stream.SetLength(file_stream.Position);
				file_stream.Seek(pak_lump_length_offset, SeekOrigin.Begin);
				binary_writer.Write(file_stream.Length - bsp_header.lumps[40].file_offset);
			}

			return AddFilesToBSPResult.Success;
		}

		//  pak lump (zip) structure:
		//      ZIP_LocalFileHeader 1
		//      [file data] 1
		//      ZIP_LocalFileHeader 2
		//      [file data] 2
		//      ...
		//      ZIP_LocalFileHeader N
		//      [file data] N
		//      ZIP_FileHeader 1
		//      ZIP_FileHeader 2
		//      ...
		//      ZIP_FileHeader N
		//      ZIP_EndOfCentralDirRecord
		public static void WriteNewPakLump(List<ZIP_File> old_files, List<ZIP_FilePathInfo> new_files, ZIP_EndOfCentralDirRecord original_end_record, string end_record_comment, BinaryWriter binary_writer)
		{
			uint pak_lump_start_offset = (uint)binary_writer.BaseStream.Position;
			uint RelativeStreamPosition()
			{
				return (uint)binary_writer.BaseStream.Position - pak_lump_start_offset;
			}

			List<ZIP_FileHeader> new_file_headers = new List<ZIP_FileHeader>(old_files.Count + new_files.Count);

			//writing old local headers and data
			for(int i = 0;i < old_files.Count;i++)
			{
				ZIP_LocalFileHeader new_local_header = new ZIP_LocalFileHeader
				{
					signature = PKID(3, 4),
					version_needed_to_extract = old_files[i].file_header.version_needed_to_extract,
					flags = old_files[i].file_header.flags,
					compression_method = old_files[i].file_header.compression_method,
					last_modified_time = old_files[i].file_header.last_modified_time,
					last_modified_date = old_files[i].file_header.last_modified_date,
					crc32 = old_files[i].file_header.crc32,
					compressed_size = old_files[i].file_header.compressed_size,
					uncompressed_size = old_files[i].file_header.uncompressed_size,
					file_name_length = old_files[i].file_header.file_name_length,
					extra_field_length = old_files[i].file_header.extra_field_length
				};

				uint local_header_offset = RelativeStreamPosition();

				binary_writer.Write(StructToBytes(new_local_header));
				binary_writer.Write(Encoding.UTF8.GetBytes(old_files[i].file_name));
				binary_writer.Write(old_files[i].extra_field);
				binary_writer.Write(old_files[i].data);

				ZIP_FileHeader new_file_header = old_files[i].file_header;
				new_file_header.relative_offset_of_local_header = local_header_offset;
				new_file_headers.Add(new_file_header);
			}

			//writing new local headers and data
			for(int i = 0;i < new_files.Count;i++)
			{
				byte[] file_data = File.ReadAllBytes(new_files[i].full_path);

				uint crc32 = Crc32.ComputeChecksum(file_data);
				uint uncompressed_size = (uint)file_data.Length;

				ZIP_LocalFileHeader new_local_header = new ZIP_LocalFileHeader
				(
					new_files[i].relative_path,
					crc32,
					uncompressed_size,
					uncompressed_size
				);

				uint local_header_offset = RelativeStreamPosition();

				binary_writer.Write(StructToBytes(new_local_header));
				binary_writer.Write(Encoding.UTF8.GetBytes(new_files[i].relative_path));
				binary_writer.Write(file_data);

				new_file_headers.Add(new ZIP_FileHeader
				(
					new_files[i].relative_path,
					crc32,
					uncompressed_size,
					uncompressed_size,
					local_header_offset
				));
			}

			uint central_directory_start = RelativeStreamPosition();

			//writing file headers (central directory)
			for(int i = 0;i < new_file_headers.Count;i++)
			{
				binary_writer.Write(StructToBytes(new_file_headers[i]));
				binary_writer.Write(Encoding.UTF8.GetBytes(i < old_files.Count ? old_files[i].file_name : new_files[i - old_files.Count].relative_path));
				if(i < old_files.Count)
				{
					binary_writer.Write(old_files[i].extra_field);
					binary_writer.Write(Encoding.UTF8.GetBytes(old_files[i].file_comment));
				}
			}

			//writing an end record
			original_end_record.central_dir_entries_total = original_end_record.central_dir_entries_this_disk = (ushort)(old_files.Count + new_files.Count);
			original_end_record.central_dir_size = RelativeStreamPosition() - central_directory_start;
			original_end_record.start_of_central_dir_offset = central_directory_start;
			binary_writer.Write(StructToBytes(original_end_record));
			binary_writer.Write(Encoding.UTF8.GetBytes(end_record_comment));
		}
	}
}