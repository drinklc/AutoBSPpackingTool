using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static AutoBSPpackingTool.Util;

namespace AutoBSPpackingTool
{
	class TimedWebClient : WebClient
	{
		public int Timeout {get; set;}

		public TimedWebClient()
		{
			Timeout = 60000;
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest request = base.GetWebRequest(address);
			request.Timeout = Timeout;
			return request;
		}
	}

	[DefaultEvent("ItemCheck")]
	public class DisableableCheckedListBox : CheckedListBox
	{
		private object CallMethod(object obj, string name, object[] parameters = null)
		{
			return obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)?.Invoke(obj, parameters);
		}

		private HashSet<int> disabled_items = new HashSet<int>();

		public void SetItemEnabled(int index, bool value)
		{
			if(index < 0 || index >= Items.Count)throw new ArgumentOutOfRangeException(nameof(index));

			if(!value)
			{
				disabled_items.Add(index);
			}
			else
			{
				disabled_items.Remove(index);
			}
			Invalidate(GetItemRectangle(index));
		}

		public bool GetItemEnabled(int index)
		{
			return !disabled_items.Contains(index);
		}

		public new void SetItemChecked(int index, bool value)
		{
			SetItemCheckState(index, value ? CheckState.Checked : CheckState.Unchecked, true);
		}

		public void SetItemCheckState(int index, CheckState value, bool ignore_disabled)
		{
			if(index < 0 || index >= Items.Count)throw new ArgumentOutOfRangeException(nameof(index));
			if(!Enum.IsDefined(typeof(CheckState), value))throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(CheckState));
			
			CheckState currentValue = GetItemCheckState(index);

			ItemCheckEventArgs itemCheckEvent = new ItemCheckEventArgs(index, value, currentValue);
			base.OnItemCheck(itemCheckEvent);

			//if(value != currentValue)
			//{
				//ItemCheckEventArgs itemCheckEvent = new ItemCheckEventArgs(index, value, currentValue);
				//OnItemCheck(itemCheckEvent, ignore_disabled);

				if(itemCheckEvent.NewValue != currentValue)
				{
					CallMethod(CheckedItems, "SetCheckedState", new object[]{index, itemCheckEvent.NewValue});
					Invalidate(GetItemRectangle(index));
				}
			//}
		}

		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			if(e.Index >= 0)
			{
				if(Font.Height < 0)
				{
					Font = DefaultFont;
				}

				string text;
				if(e.Index < Items.Count)
				{
					text = Items[e.Index].ToString();
				}
				else
				{
					text = (string)CallMethod(this, "NativeGetItemText", new object[]{e.Index});
				}

				Rectangle bounds = e.Bounds;
				int height = ItemHeight;

				CheckBoxState checkbox_state = e.Index < Items.Count ? GetItemEnabled(e.Index) ? (GetItemChecked(e.Index) ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal) : (GetItemChecked(e.Index) ? CheckBoxState.CheckedDisabled : CheckBoxState.UncheckedDisabled) : CheckBoxState.UncheckedNormal;

				int ideal_check_size = CheckBoxRenderer.GetGlyphSize(e.Graphics, checkbox_state).Width;

				int centering_factor = Math.Max((height - ideal_check_size) / 2, 0);
				if(centering_factor + ideal_check_size > bounds.Height)
				{
					centering_factor = bounds.Height - ideal_check_size;
				}

				int list_item_start_position = 1;

				Rectangle box = new Rectangle(bounds.X + list_item_start_position, bounds.Y + centering_factor, ideal_check_size, ideal_check_size);
				if(RightToLeft == RightToLeft.Yes)
				{
					box.X = bounds.X + bounds.Width - ideal_check_size - list_item_start_position;
				}

				Rectangle text_bounds = new Rectangle(bounds.X + ideal_check_size + (list_item_start_position * 2), bounds.Y, bounds.Width - (ideal_check_size + (list_item_start_position * 2)), bounds.Height);
				if(RightToLeft == RightToLeft.Yes)
				{
					text_bounds.X = bounds.X;
				}

				Color disabled_selected_backcolor = SystemColors.Control;
				Color back_color = SelectionMode != SelectionMode.None ? (e.State.HasFlag(DrawItemState.Selected) && (!GetItemEnabled(e.Index) || !Enabled) ? disabled_selected_backcolor : e.BackColor) : BackColor;
				Color fore_color = !GetItemEnabled(e.Index) || !Enabled ? SystemColors.GrayText : (SelectionMode != SelectionMode.None ? e.ForeColor : ForeColor);

				using(SolidBrush brush = new SolidBrush(back_color))
				{
					e.Graphics.FillRectangle(brush, text_bounds); //draws a background
				}

				CheckBoxRenderer.DrawCheckBox(e.Graphics, box.Location, checkbox_state);

				int BORDER_SIZE = 1;
				Rectangle string_bounds = new Rectangle(text_bounds.X + BORDER_SIZE, text_bounds.Y, text_bounds.Width - BORDER_SIZE, text_bounds.Height - 2 * BORDER_SIZE);

				TextFormatFlags flags = TextFormatFlags.NoPrefix;
				if(UseTabStops || UseCustomTabOffsets)
				{
				    flags |= TextFormatFlags.ExpandTabs;
				}
				if(RightToLeft == RightToLeft.Yes)
				{
				    flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;
				}

				TextRenderer.DrawText(e.Graphics, text, Font, string_bounds, fore_color, back_color, flags);

				if(e.State.HasFlag(DrawItemState.Focus) && !e.State.HasFlag(DrawItemState.NoFocusRect))
				{
					ControlPaint.DrawFocusRectangle(e.Graphics, text_bounds, fore_color, back_color);
				}
			}
		}

		protected override void OnItemCheck(ItemCheckEventArgs ice)
		{
			if(disabled_items.Contains(ice.Index))
			{
				ice.NewValue = ice.CurrentValue;
			}

			base.OnItemCheck(ice);
		}

		protected void OnItemCheck(ItemCheckEventArgs ice, bool ignore_disabled = false)
		{
			if(!ignore_disabled && disabled_items.Contains(ice.Index))
			{
				ice.NewValue = ice.CurrentValue;
			}

			base.OnItemCheck(ice);
		}
	}

	[DefaultEvent("ItemReordered")]
	public class ReorderableListBox : ListBox
	{
		public ReorderableListBox()
		{
			AllowDrop = true;
			DrawMode = DrawMode.OwnerDrawFixed;
		}

		[DefaultValue(true)]
		public new bool AllowDrop
		{
			get => base.AllowDrop;
			set => base.AllowDrop = value;
		}

		[DefaultValue(DrawMode.OwnerDrawFixed)]
		public new DrawMode DrawMode
		{
			get => base.DrawMode;
			set => base.DrawMode = value;
		}

		private int drag_start_index;
		private int drag_current_index;
		private object unique_drag_item = new object();
		private object drag_item;
		private ListBoxSeparatedItem drag_separated_item = null;
		private Point drag_start_location;

		[Category("Behavior")]
		public event EventHandler ItemReordered;

		private void DrawHandle(Graphics graphics, Rectangle bounds) //draws a reorder handle
		{
			int start_x = bounds.Right - 4 * 3;
			int start_y = bounds.Y + (bounds.Height - (2 * 3 - 1)) / 2;
			for(int y = 0;y < 2;y++)
			{
				for(int x = 0;x < 4;x++)
				{
					graphics.FillRectangle(SystemBrushes.ControlLight, start_x + x * 3, start_y + y * 3, 1, 1);
					graphics.FillRectangle(SystemBrushes.ControlDarkDark, start_x + x * 3, start_y + y * 3 + 1, 1, 1);
				}
			}
		}

		private class ListBoxSeparatedItem : Control
		{
			public ListBoxSeparatedItem(ReorderableListBox owner)
			{
				this.owner = owner;
			}

			private ReorderableListBox owner;
			public string item;

			protected override void WndProc(ref Message m)
			{
				const int WM_NCHITTEST = 0x84;
				const int HTTRANSPARENT = -1;
				if(m.Msg == WM_NCHITTEST)
				{
					m.Result = (IntPtr)HTTRANSPARENT;
					return;
				}
				base.WndProc(ref m);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				e.Graphics.FillRectangle(SystemBrushes.Highlight, e.ClipRectangle);

				TextFormatFlags flags = TextFormatFlags.NoPrefix;
				if(owner.UseTabStops || owner.UseCustomTabOffsets)
				{
				    flags |= TextFormatFlags.ExpandTabs;
				}
				if(RightToLeft == RightToLeft.Yes)
				{
				    flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;
				}

				Rectangle text_bounds = new Rectangle(e.ClipRectangle.X - 1, e.ClipRectangle.Y, e.ClipRectangle.Width + 1 - (4 * 3 + 2), e.ClipRectangle.Height);
				TextRenderer.DrawText(e.Graphics, item, owner.Font, text_bounds, SystemColors.HighlightText, SystemColors.Highlight, flags);

				owner.DrawHandle(e.Graphics, e.ClipRectangle);
			}
		}

		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			base.OnDrawItem(e);

			if(e.Index >= 0 && e.Index < Items.Count)
			{
				Color disabled_selected_backcolor = SystemColors.Control;
				Color back_color = SelectionMode != SelectionMode.None ? (e.State.HasFlag(DrawItemState.Selected) && !Enabled ? disabled_selected_backcolor : e.BackColor) : BackColor;
				Color fore_color = !Enabled ? SystemColors.GrayText : (SelectionMode != SelectionMode.None ? e.ForeColor : ForeColor);;

				using(SolidBrush brush = new SolidBrush(back_color))
				{
					e.Graphics.FillRectangle(brush, e.Bounds); //draws a background
				}

				TextFormatFlags flags = TextFormatFlags.NoPrefix;
				if(UseTabStops || UseCustomTabOffsets)
				{
				    flags |= TextFormatFlags.ExpandTabs;
				}
				if(RightToLeft == RightToLeft.Yes)
				{
				    flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;
				}

				Rectangle text_bounds = new Rectangle(e.Bounds.X - 1, e.Bounds.Y, e.Bounds.Width + 1 - (4 * 3 + 2), e.Bounds.Height);
				TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), e.Font, text_bounds, fore_color, back_color, flags);

				if(drag_separated_item == null || e.Index != drag_current_index)
				{
					DrawHandle(e.Graphics, e.Bounds);
				}

				e.DrawFocusRectangle();
			}
		}

		public new int IndexFromPoint(Point point)
		{
			return IndexFromPoint(point.X, point.Y);
		}

		public new int IndexFromPoint(int x, int y)
		{
            for(int i = 0;i < Items.Count;i++)
			{
				Rectangle current_rectangle = GetItemRectangle(i);
				current_rectangle.X -= 2;
				current_rectangle.Width += 4;
				if(i == 0)
				{
					current_rectangle.Y -= 2;
					current_rectangle.Height += 2;
				}
				if(current_rectangle.Contains(x, y))return i;
			}
            return NoMatches;
        }

		private void RemoveDragSeparatedItem()
		{
			Controls.Remove(drag_separated_item);
			drag_separated_item.Dispose();
			drag_separated_item = null;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			Rectangle handles_rectangle = new Rectangle(ClientRectangle.Right - (4 * 3 + 2), ClientRectangle.Y, 4 * 3 + 2, ClientRectangle.Height);
			int selected_index = IndexFromPoint(e.Location);
			if(e.Button == MouseButtons.Left && handles_rectangle.Contains(e.Location) && selected_index != -1)
			{
				drag_current_index = drag_start_index = selected_index;
				drag_item = Items[selected_index];
				Rectangle item_rectangle = GetItemRectangle(selected_index);
				drag_start_location = new Point(e.Location.X - item_rectangle.Location.X, e.Location.Y - item_rectangle.Location.Y);
				DoDragDrop(unique_drag_item, DragDropEffects.Move);
			}

			base.OnMouseDown(e);
		}

		protected override void OnDragEnter(DragEventArgs drgevent)
		{
			if(drgevent.Data.GetData(typeof(object)) == unique_drag_item)
			{
				drgevent.Effect = DragDropEffects.Move;

				Rectangle item_rectangle = GetItemRectangle(drag_start_index);
				Point client_point = PointToClient(new Point(drgevent.X, drgevent.Y));

				drag_separated_item = new ListBoxSeparatedItem(this)
				{
					Location = new Point(item_rectangle.Location.X, client_point.Y - drag_start_location.Y),
					Size = item_rectangle.Size,
					item = drag_item.ToString()
				};
				Controls.Add(drag_separated_item);

				Items[drag_current_index] = "";
				SelectedIndex = -1;
			}

			base.OnDragEnter(drgevent);
		}

		protected override void OnDragLeave(EventArgs e)
		{
			if(drag_separated_item != null)
			{
				Items.RemoveAt(drag_current_index);
				Items.Insert(drag_start_index, drag_item);
				SelectedIndex = drag_start_index;
				drag_current_index = drag_start_index;
				RemoveDragSeparatedItem();
			}

			base.OnDragLeave(e);
		}

		protected override void OnDragOver(DragEventArgs drgevent)
		{
			if(drgevent.Data.GetData(typeof(object)) == unique_drag_item)
			{
				Point client_point = PointToClient(new Point(drgevent.X, drgevent.Y));
				int new_index = IndexFromPoint(0, drag_separated_item.Location.Y + drag_separated_item.Height / 2);
				if(new_index == NoMatches)new_index = Items.Count - 1;
				if(drag_current_index != new_index)
				{
					Items.RemoveAt(drag_current_index);
					Items.Insert(new_index, "");
					drag_current_index = new_index;
				}
				drag_separated_item.Location = new Point(drag_separated_item.Location.X, Clamp(client_point.Y - drag_start_location.Y, 0, ClientRectangle.Height - GetItemHeight(Items.Count - 1)));
			}

			base.OnDragOver(drgevent);
		}

		protected override void OnDragDrop(DragEventArgs drgevent)
		{
			if(drgevent.Data.GetData(typeof(object)) == unique_drag_item)
			{
				Items.RemoveAt(drag_current_index);
				Items.Insert(drag_current_index, drag_item);
				SelectedIndex = drag_current_index;
				RemoveDragSeparatedItem();
				if(drag_current_index != drag_start_index)OnItemReordered(EventArgs.Empty);
			}

			base.OnDragDrop(drgevent);
		}

		protected virtual void OnItemReordered(EventArgs e)
		{
			ItemReordered?.Invoke(this, e);
		}
	}

	class FolderPicker
	{
		private readonly List<string> _resultPaths = new List<string>();
		private readonly List<string> _resultNames = new List<string>();

		public /*IReadOnlyList*/IEnumerable<string> ResultPaths => _resultPaths; //IReadOnlyList is not available in .NET Framework 4.0
		public /*IReadOnlyList*/IEnumerable<string> ResultNames => _resultNames;
		public string ResultPath => ResultPaths.FirstOrDefault();
		public string ResultName => ResultNames.FirstOrDefault();
		public virtual string InitialDirectory { get; set; }
		public virtual bool ForceFileSystem { get; set; }
		public virtual bool Multiselect { get; set; }
		public virtual string Title { get; set; }
		public virtual string OkButtonLabel { get; set; }
		public virtual string FileNameLabel { get; set; }

		protected virtual int SetOptions(int options)
		{
			if(ForceFileSystem)
			{
				options |= (int)FOS.FOS_FORCEFILESYSTEM;
			}

			if(Multiselect)
			{
				options |= (int)FOS.FOS_ALLOWMULTISELECT;
			}
			return options;
		}

		public virtual bool? ShowDialog(IntPtr owner, bool throwOnError = false)
		{
			var dialog = (IFileOpenDialog)new FileOpenDialog();
			if(!string.IsNullOrEmpty(InitialDirectory))
			{
				if(CheckHr(SHCreateItemFromParsingName(InitialDirectory, null, typeof(IShellItem).GUID, out var item), throwOnError) == 0)
				{
					dialog.SetFolder(item);
				}
			}

			var options = FOS.FOS_PICKFOLDERS;
			options = (FOS)SetOptions((int)options);
			dialog.SetOptions(options);

			if(Title != null)
			{
				dialog.SetTitle(Title);
			}

			if(OkButtonLabel != null)
			{
				dialog.SetOkButtonLabel(OkButtonLabel);
			}

			if(FileNameLabel != null)
			{
				dialog.SetFileName(FileNameLabel);
			}

			if(owner == IntPtr.Zero)
			{
				owner = Process.GetCurrentProcess().MainWindowHandle;
				if(owner == IntPtr.Zero)
				{
					owner = GetDesktopWindow();
				}
			}

			var hr = dialog.Show(owner);
			if(hr == ERROR_CANCELLED)return null;

			if(CheckHr(hr, throwOnError) != 0)return null;

			if(CheckHr(dialog.GetResults(out var items), throwOnError) != 0)return null;

			items.GetCount(out var count);
			for(var i = 0;i < count;i++)
			{
				items.GetItemAt(i, out var item);
				CheckHr(item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out var path), throwOnError);
				CheckHr(item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEEDITING, out var name), throwOnError);
				if(path != null || name != null)
				{
					_resultPaths.Add(path);
					_resultNames.Add(name);
				}
			}
			return true;
		}

		private static int CheckHr(int hr, bool throwOnError)
		{
			if(hr != 0 && throwOnError)Marshal.ThrowExceptionForHR(hr);
			return hr;
		}

		[DllImport("shell32")]
		private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IBindCtx pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);

		[DllImport("user32")]
		private static extern IntPtr GetDesktopWindow();

		private const int ERROR_CANCELLED = unchecked((int)0x800704C7);

		[ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")] //CLSID_FileOpenDialog
		private class FileOpenDialog { }

		[ComImport, Guid("d57c7288-d4ad-4768-be02-9d969532d960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IFileOpenDialog
		{
			[PreserveSig] int Show(IntPtr parent); //IModalWindow
			[PreserveSig] int SetFileTypes();  //not fully defined
			[PreserveSig] int SetFileTypeIndex(int iFileType);
			[PreserveSig] int GetFileTypeIndex(out int piFileType);
			[PreserveSig] int Advise(/*IFileDialogEvents pfde, out uint pdwCookie*/); //not fully defined
			[PreserveSig] int Unadvise(/*uint dwCookie*/);
			[PreserveSig] int SetOptions(FOS fos);
			[PreserveSig] int GetOptions(out FOS pfos);
			[PreserveSig] int SetDefaultFolder(IShellItem psi);
			[PreserveSig] int SetFolder(IShellItem psi);
			[PreserveSig] int GetFolder(out IShellItem ppsi);
			[PreserveSig] int GetCurrentSelection(out IShellItem ppsi);
			[PreserveSig] int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
			[PreserveSig] int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
			[PreserveSig] int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
			[PreserveSig] int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
			[PreserveSig] int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
			[PreserveSig] int GetResult(out IShellItem ppsi);
			[PreserveSig] int AddPlace(IShellItem psi, int alignment);
			[PreserveSig] int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
			[PreserveSig] int Close(int hr);
			[PreserveSig] int SetClientGuid();  //not fully defined
			[PreserveSig] int ClearClientData();
			[PreserveSig] int SetFilter([MarshalAs(UnmanagedType.IUnknown)] object pFilter);
			[PreserveSig] int GetResults(out IShellItemArray ppenum);
			[PreserveSig] int GetSelectedItems([MarshalAs(UnmanagedType.IUnknown)] out object ppsai);
		}

		[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IShellItem
		{
			[PreserveSig] int BindToHandler(); //not fully defined
			[PreserveSig] int GetParent(); //not fully defined
			[PreserveSig] int GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
			[PreserveSig] int GetAttributes();  //not fully defined
			[PreserveSig] int Compare();  //not fully defined
		}

		[ComImport, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IShellItemArray
		{
			[PreserveSig] int BindToHandler();  //not fully defined
			[PreserveSig] int GetPropertyStore();  //not fully defined
			[PreserveSig] int GetPropertyDescriptionList();  //not fully defined
			[PreserveSig] int GetAttributes();  //not fully defined
			[PreserveSig] int GetCount(out int pdwNumItems);
			[PreserveSig] int GetItemAt(int dwIndex, out IShellItem ppsi);
			[PreserveSig] int EnumItems();  //not fully defined
		}

		/*[ComImport, Guid("973510DB-7D7F-452B-8975-74A85828D354"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IFileDialogEvents
		{
			//NOTE: some of these callbacks are cancelable - returning S_FALSE means that 
			//the dialog should not proceed (e.g. with closing, changing folder); to 
			//support this, we need to use the PreserveSig attribute to enable us to return
			//the proper HRESULT
			[PreserveSig] int OnFileOk([In, MarshalAs(UnmanagedType.Interface)] IFileOpenDialog pfd);
		
			[PreserveSig] int OnFolderChanging([In, MarshalAs(UnmanagedType.Interface)] IFileOpenDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psiFolder);
		
			void OnFolderChange([In, MarshalAs(UnmanagedType.Interface)] IFileOpenDialog pfd);
		
			void OnSelectionChange([In, MarshalAs(UnmanagedType.Interface)] IFileOpenDialog pfd);
		
			void OnShareViolation([In, MarshalAs(UnmanagedType.Interface)] IFileOpenDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE pResponse);
		
			void OnTypeChange([In, MarshalAs(UnmanagedType.Interface)] IFileOpenDialog pfd);
		
			void OnOverwrite([In, MarshalAs(UnmanagedType.Interface)] IFileOpenDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out FDE_OVERWRITE_RESPONSE pResponse);
		}*/

		private enum SIGDN : uint
		{
			SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
			SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
			SIGDN_FILESYSPATH = 0x80058000,
			SIGDN_NORMALDISPLAY = 0,
			SIGDN_PARENTRELATIVE = 0x80080001,
			SIGDN_PARENTRELATIVEEDITING = 0x80031001,
			SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
			SIGDN_PARENTRELATIVEPARSING = 0x80018001,
			SIGDN_URL = 0x80068000
		}

		[Flags]
		private enum FOS
		{
			FOS_OVERWRITEPROMPT = 0x2,
			FOS_STRICTFILETYPES = 0x4,
			FOS_NOCHANGEDIR = 0x8,
			FOS_PICKFOLDERS = 0x20,
			FOS_FORCEFILESYSTEM = 0x40,
			FOS_ALLNONSTORAGEITEMS = 0x80,
			FOS_NOVALIDATE = 0x100,
			FOS_ALLOWMULTISELECT = 0x200,
			FOS_PATHMUSTEXIST = 0x800,
			FOS_FILEMUSTEXIST = 0x1000,
			FOS_CREATEPROMPT = 0x2000,
			FOS_SHAREAWARE = 0x4000,
			FOS_NOREADONLYRETURN = 0x8000,
			FOS_NOTESTFILECREATE = 0x10000,
			FOS_HIDEMRUPLACES = 0x20000,
			FOS_HIDEPINNEDPLACES = 0x40000,
			FOS_NODEREFERENCELINKS = 0x100000,
			FOS_OKBUTTONNEEDSINTERACTION = 0x200000,
			FOS_DONTADDTORECENT = 0x2000000,
			FOS_FORCESHOWHIDDEN = 0x10000000,
			FOS_DEFAULTNOMINIMODE = 0x20000000,
			FOS_FORCEPREVIEWPANEON = 0x40000000,
			FOS_SUPPORTSTREAMABLEITEMS = unchecked((int)0x80000000)
		}

		/*private enum FDE_SHAREVIOLATION_RESPONSE
		{
			FDESVR_DEFAULT = 0x00000000,
			FDESVR_ACCEPT = 0x00000001,
			FDESVR_REFUSE = 0x00000002
		}

		private enum FDE_OVERWRITE_RESPONSE
		{
			FDEOR_DEFAULT = 0x00000000,
			FDEOR_ACCEPT = 0x00000001,
			FDEOR_REFUSE = 0x00000002
		}*/
	}

	static class TaskbarProgress
	{
		public enum TaskbarStates
		{
			NoProgress = 0,
			Indeterminate = 0x1,
			Normal = 0x2,
			Error = 0x4,
			Paused = 0x8
		}

		[ComImportAttribute()]
		[GuidAttribute("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
		[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
		private interface ITaskbarList3
		{
			//ITaskbarList
			[PreserveSig]
			void HrInit();
			[PreserveSig]
			void AddTab(IntPtr hwnd);
			[PreserveSig]
			void DeleteTab(IntPtr hwnd);
			[PreserveSig]
			void ActivateTab(IntPtr hwnd);
			[PreserveSig]
			void SetActiveAlt(IntPtr hwnd);

			//ITaskbarList2
			[PreserveSig]
			void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

			//ITaskbarList3
			[PreserveSig]
			void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
			[PreserveSig]
			void SetProgressState(IntPtr hwnd, TaskbarStates state);
		}

		[GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
		[ClassInterfaceAttribute(ClassInterfaceType.None)]
		[ComImportAttribute()]
		private class TaskbarInstance
		{
		}

		private static ITaskbarList3 taskbarInstance = (ITaskbarList3)new TaskbarInstance();

		public static void SetState(IntPtr windowHandle, TaskbarStates taskbarState)
		{
			taskbarInstance.SetProgressState(windowHandle, taskbarState);
		}

		public static void SetValue(IntPtr windowHandle, double progressValue, double progressMax)
		{
			taskbarInstance.SetProgressValue(windowHandle, (ulong)progressValue, (ulong)progressMax);
		}
	}
}