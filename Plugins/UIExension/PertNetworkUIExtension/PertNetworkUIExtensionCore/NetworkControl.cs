﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;

namespace PertNetworkUIExtension
{
	public delegate void SelectionChangeEventHandler(object sender, NetworkItem item);
	public delegate bool DragDropChangeEventHandler(object sender, NetworkDragEventArgs e);

	[System.ComponentModel.DesignerCategory("")]

	public class NetworkDragEventItem
	{
		public NetworkDragEventItem( /* TODO*/ )
		{
		}
	}

	public class NetworkDragEventArgs : EventArgs
	{
		public NetworkDragEventArgs( /* TODO*/ )
		{
		}

	}

	public partial class NetworkControl : UserControl
	{
		// Win32 Imports -----------------------------------------------------------------

		[DllImport("User32.dll")]
		static extern int SendMessage(IntPtr hWnd, int msg, int wParam = 0, int lParam = 0);

		// --------------------------

		[DllImport("User32.dll")]
		static extern uint GetDoubleClickTime();
		
		// --------------------------

		[DllImport("User32.dll")]
		static extern int GetSystemMetrics(int index);
		
		const int SM_CXDOUBLECLK = 36;
		const int SM_CYDOUBLECLK = 37;
		const int SM_CXDRAG = 68;
		const int SM_CYDRAG = 69;

		const int WM_PAINT = 0x000F;

		// Constants ---------------------------------------------------------------------

		virtual protected int ScaleByDPIFactor(int value)
        {
            return value;
        }

		protected float ZoomFactor { get; private set; }
		protected bool IsZoomed { get { return (ZoomFactor < 1.0f); } }

		protected enum DropPos
        {
            None,
            On,
            Left,
            Right,
        }

		// Data --------------------------------------------------------------------------

        private DropPos DropSite;
		private Timer DragTimer;
		private int LastDragTick = 0;

        private bool IsSavingToImage = false;
		private uint SelectedItemId = 0;

// #if DEBUG
// 		private int RecalcDuration;
// #endif

		protected int ItemHeight = 30;
		protected int ItemWidth  = 150;
		protected int ItemVertSpacing = 10;
		protected int ItemHorzSpacing = 30;
		protected int GraphBorder = 20;

		public NetworkData Data
		{
			get; private set;
		}

		// Public ------------------------------------------------------------------------

		public event SelectionChangeEventHandler SelectionChange;
		public event DragDropChangeEventHandler DragDropChange;

        public NetworkControl()
        {
			DropSite = DropPos.None;
			ConnectionColor = Color.Magenta;

			Data = new NetworkData();

			InitializeComponent();
		}

		public void RebuildGroups()
		{
			Point maxPos = Data.RebuildGroups();

			Rectangle maxItemRect = CalcItemRectangle(maxPos.X, maxPos.Y);

			this.AutoScrollMinSize = new Size(maxItemRect.Right + GraphBorder, maxItemRect.Bottom + GraphBorder);

			this.HorizontalScroll.SmallChange = ItemHeight;
			this.VerticalScroll.SmallChange = ItemWidth;

			Invalidate();
		}

		public NetworkItem HitTestItem(Point pos)
		{
			// Brute force for now
			foreach (var group in Data.Groups)
			{
				foreach (var item in group.ItemValues)
				{
					var itemRect = CalcItemRectangle(item);

					if (itemRect.Contains(pos))
						return item;
				}
			}

			// else
			return null;
		}
		
		protected override void OnPaint(PaintEventArgs e)
		{
			int iGroup = 0;
			var drawnItems = new HashSet<NetworkItem>();

			foreach (var group in Data.Groups)
			{
				foreach (var item in group.ItemValues)
				{
					if (!drawnItems.Contains(item))
					{
						if (WantDrawItem(e, item))
						{
							OnPaintItem(e.Graphics, item, iGroup, (item.UniqueId == SelectedItemId));
						}
						else
						{
							//int breakpoint = 0;
						}

						var dependencies = group.GetItemDependencies(item);

						foreach (var dependItem in dependencies)
						{
							if (WantDrawConnection(e, dependItem, item))
							{
								var smoothing = e.Graphics.SmoothingMode;
								e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

								OnPaintConnection(e.Graphics, dependItem, item);

								e.Graphics.SmoothingMode = smoothing;
							}
							else
							{
								//int breakpoint = 0;
							}
						}

						drawnItems.Add(item);
					}
				}

				iGroup++;
			}
		}

		bool WantDrawItem(PaintEventArgs e, NetworkItem item)
		{
			var itemRect = CalcItemRectangle(item);

			return e.ClipRectangle.IntersectsWith(itemRect);
		}

		bool WantDrawConnection(PaintEventArgs e, NetworkItem fromItem, NetworkItem toItem)
		{
			var fromRect = CalcItemRectangle(fromItem);
			var toRect = CalcItemRectangle(toItem);

			return e.ClipRectangle.IntersectsWith(Rectangle.Union(fromRect, toRect));
		}

		virtual protected void OnPaintItem(Graphics graphics, NetworkItem item, int iGroup, bool selected)
		{
			var itemRect = CalcItemRectangle(item);
			var itemText = String.Format("{0} (id: {1}, grp: {2})", item.Title, item.UniqueId, iGroup);

			graphics.DrawRectangle(Pens.Black, itemRect);

			if (selected)
			{
				graphics.FillRectangle(SystemBrushes.Highlight, itemRect);
				graphics.DrawString(itemText, this.Font, SystemBrushes.HighlightText, itemRect);
			}
			else
			{
				graphics.DrawString(itemText, this.Font, Brushes.Blue, itemRect);
			}
		}

		virtual protected void OnPaintConnection(Graphics graphics, NetworkItem fromItem, NetworkItem toItem)
		{
			var fromRect = CalcItemRectangle(fromItem);
			var toRect = CalcItemRectangle(toItem);

			graphics.DrawLine(Pens.Blue, 
								fromRect.Right, 
								(fromRect.Top + (fromRect.Height / 2)), 
								toRect.Left, 
								(toRect.Top + (toRect.Height / 2)));
		}

		virtual protected Rectangle CalcItemRectangle(NetworkItem item)
		{
			return CalcItemRectangle(item.Position.X, item.Position.Y);
		}

		virtual protected Rectangle CalcItemRectangle(int x, int y)
		{
			Rectangle itemRect = new Rectangle(GraphBorder, GraphBorder, 0, 0);

			itemRect.X += ((ItemWidth + ItemHorzSpacing) * x);
			itemRect.Y += ((ItemHeight + ItemVertSpacing) * y);

			itemRect.Width = ItemWidth;
			itemRect.Height = ItemHeight;

			// Offset rect by scroll pos
			itemRect.Offset(-this.HorizontalScroll.Value, -this.VerticalScroll.Value);

			return itemRect;
		}
		
		public void SetFont(String fontName, int fontSize)
        {
            if ((this.Font.Name == fontName) && (this.Font.Size == fontSize))
                return;

            this.Font = new Font(fontName, fontSize, FontStyle.Regular);
        }

		public Color ConnectionColor;
		public bool ReadOnly;

        public bool SetSelectedItem(uint uniqueID)
        {
            var item = Data.GetItem(uniqueID);

			if (item == null)
				return false;

            SelectedItemId = uniqueID;

			EnsureItemVisible(item);
			Invalidate();

            return true;
        }

		public Rectangle GetSelectedItemRect()
		{
			var selItem = SelectedItem;

			return ((selItem == null) ? new Rectangle(0, 0, 0, 0) : CalcItemRectangle(selItem));
		}

        public Bitmap SaveToImage()
        {
			// Cache state
			/*
						Point scrollPos = new Point(HorizontalScroll.Value, VerticalScroll.Value);
						Point drawOffset = new Point(DrawOffset.X, DrawOffset.Y);

						// And reset
						IsSavingToImage = true;
						DrawOffset = new Point(0, 0);

						HorizontalScroll.Value = 0;
						VerticalScroll.Value = 0;

						if (!scrollPos.IsEmpty)
							PerformLayout();

						int border = BorderWidth;

						// Total size of the graph
						Rectangle graphRect = Rectangle.Inflate(RootItem.TotalBounds, GraphPadding, GraphPadding);

						// The portion of the client rect we are interested in 
						// (excluding the top and left borders)
						Rectangle srcRect = Rectangle.FromLTRB(border, 
															   border, 
															   ClientRectangle.Width - border, 
															   ClientRectangle.Height - border);

						// The output image
						Bitmap finalImage = new Bitmap(graphRect.Width, graphRect.Height, PixelFormat.Format32bppRgb);

						// The temporary image allowing us to clip out the top and left borders
						Bitmap srcImage = new Bitmap(ClientRectangle.Width, ClientRectangle.Height, PixelFormat.Format32bppRgb);

						// The current position in the output image for rendering the temporary image
						Rectangle drawRect = srcRect;
						drawRect.Offset(-border, -border);

						// Note: If the last horz or vert page is empty because of an 
						// exact division then it will get handled within the loop
						int numHorzPages = ((graphRect.Width / drawRect.Width) + 1);
						int numVertPages = ((graphRect.Height / drawRect.Height) + 1);

						using (Graphics graphics = Graphics.FromImage(finalImage))
						{
							for (int vertPage = 0; vertPage < numVertPages; vertPage++)
							{
								for (int horzPage = 0; horzPage < numHorzPages; horzPage++)
								{
									// TODO
								}

								// TODO
							}
						}

						// Restore state
						IsSavingToImage = false;
						DrawOffset = drawOffset;

						HorizontalScroll.Value = scrollPos.X;
						VerticalScroll.Value = scrollPos.Y;

						if (!scrollPos.IsEmpty)
							PerformLayout();

						return finalImage;
			*/
			return null;
        }

		// Message Handlers -----------------------------------------------------------

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
			// TODO
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
			base.OnMouseDown(e);

			if (e.Button != MouseButtons.Left)
				return;

			var item = HitTestItem(e.Location);

			if (item == null)
				return;

			if (item.UniqueId != SelectedItemId)
			{
				SetSelectedItem(item.UniqueId);

				SelectionChange?.Invoke(this, SelectedItem);
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if ((ModifierKeys & Keys.Control) == Keys.Control)
			{
/*
				float newFactor = ZoomFactor;

				if (e.Delta > 0)
				{
					newFactor += 0.1f;
					newFactor = Math.Min(newFactor, 1.0f);
				}
				else
				{
					newFactor -= 0.1f;
					newFactor = Math.Max(newFactor, 0.4f);
				}

				if (newFactor != ZoomFactor)
				{
					Cursor = Cursors.WaitCursor;

					// Convert mouse pos to relative coordinates
					float relX = ((e.Location.X + HorizontalScroll.Value) / (float)HorizontalScroll.Maximum);
					float relY = ((e.Location.Y + VerticalScroll.Value) / (float)VerticalScroll.Maximum);

					// Prevent all selection and expansion changes for the duration
 					BeginUpdate();

					// The process of changing the fonts and recalculating the 
					// item height can cause the tree-view to spontaneously 
					// collapse tree items so we save the expansion state
					// and restore it afterwards
					var expandedItems = GetExpandedItems(RootItem);

					ZoomFactor = newFactor;
					UpdateTreeFont(false);

					// 'Cleanup'
					SetExpandedItems(expandedItems);
 					EndUpdate();

					// Scroll the view to keep the mouse located in the 
					// same relative position as before
					if (HorizontalScroll.Visible)
					{
						int newX = (int)(relX * HorizontalScroll.Maximum) - e.Location.X;

						HorizontalScroll.Value = Validate(newX, HorizontalScroll);
					}

					if (VerticalScroll.Visible)
					{
						int newY = (int)(relY * VerticalScroll.Maximum) - e.Location.Y;

						VerticalScroll.Value = Validate(newY, VerticalScroll);
					}

					PerformLayout();
				}
*/
			}
			else
			{
				// Default scroll
				base.OnMouseWheel(e);
			}
		}

		static int Validate(int pos, ScrollProperties scroll)
		{
			return Math.Max(scroll.Minimum, Math.Min(pos, scroll.Maximum));
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if ((e.Button == MouseButtons.Left) && DragTimer.Enabled)
			{
                Debug.Assert(!ReadOnly);

				if (CheckStartDragging(e.Location))
					DragTimer.Stop();
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			DragTimer.Stop();

			base.OnMouseUp(e);

			if (e.Button == MouseButtons.Left)
			{
				// TODO
			}
		}

		protected void OnDragTimer(object sender, EventArgs e)
		{
            Debug.Assert(!ReadOnly);

			DragTimer.Stop();

			bool mouseDown = ((MouseButtons & MouseButtons.Left) == MouseButtons.Left);

			if (mouseDown)
				CheckStartDragging(MousePosition);
		}
	
		protected override void OnDragOver(DragEventArgs e)
		{
            Debug.Assert(!ReadOnly);

			// TODO
		}

		protected override void OnDragDrop(DragEventArgs e)
		{
            Debug.Assert(!ReadOnly);

			// TODO
		}

		protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
		{
            Debug.Assert(!ReadOnly);

            base.OnQueryContinueDrag(e);

			if (e.EscapePressed)
			{
				// TODO
			}
		}

		protected override void OnDragLeave(EventArgs e)
		{
            Debug.Assert(!ReadOnly);

            base.OnDragLeave(e);

			// TODO

			Invalidate();
		}
		
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			Invalidate(GetSelectedItemRect());
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);

			Invalidate(GetSelectedItemRect());
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			
			// Always reset the logical zoom else we've no way of 
			// accurately calculating the actual zoom
			ZoomFactor = 1f;

			//UpdateTreeFont(true);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (HandleCursorKey(e.KeyCode))
				return;

// 			switch (e.KeyCode)
// 			{
// 			}

			// else
			base.OnKeyDown(e);
		}

        // Internals -----------------------------------------------------------

/*
        private int BorderWidth
        {
            get
            {
                switch (BorderStyle)
                {
                    case BorderStyle.FixedSingle:
                        return 1;

                    case BorderStyle.Fixed3D:
                        return 2;
                }

                return 0;
            }
        }
*/

		private bool CheckStartDragging(Point cursor)
		{
            Debug.Assert(!ReadOnly);

			// TODO

			return false;
		}
		
		virtual protected bool IsAcceptableDropTarget(Object draggedItemData, Object dropTargetItemData, DropPos dropPos, bool copy)
		{
            Debug.Assert(!ReadOnly);

            return true;
		}

		virtual protected bool IsAcceptableDragSource(Object itemData)
		{
            Debug.Assert(!ReadOnly);

            return (itemData != null);
		}

		private Rectangle GetDoubleClickRect(Point cursor)
		{
			var rect = new Rectangle(cursor.X, cursor.Y, 0, 0);
			rect.Inflate(GetSystemMetrics(SM_CXDOUBLECLK) / 2, GetSystemMetrics(SM_CYDOUBLECLK) / 2);

			return rect;
		}

		private Rectangle GetDragRect(Point cursor)
		{
            Debug.Assert(!ReadOnly);

            var rect = new Rectangle(cursor.X, cursor.Y, 0, 0);
			rect.Inflate(GetSystemMetrics(SM_CXDRAG) / 2, GetSystemMetrics(SM_CYDRAG) / 2);

			return rect;
		}

		private void DoDrop(/* TODO */)
		{
            Debug.Assert(!ReadOnly);

			// TODO
		}

		virtual protected bool DoDrop(NetworkDragEventArgs e)
		{
            Debug.Assert(!ReadOnly);

            if (DragDropChange != null)
				return DragDropChange(this, e);

			// else
			return true;
		}

		protected void EnsureItemVisible(NetworkItem item)
        {
            if (item == null)
                return;

            Rectangle itemRect = CalcItemRectangle(item);

            if (ClientRectangle.Contains(itemRect))
                return;

            if (HorizontalScroll.Visible)
            {
                int xOffset = 0;

                if (itemRect.Left < ClientRectangle.Left)
                {
                    xOffset = (itemRect.Left - ClientRectangle.Left - (ItemHorzSpacing / 2));
                }
                else if (itemRect.Right > ClientRectangle.Right)
                {
                    xOffset = (itemRect.Right - ClientRectangle.Right + (ItemHorzSpacing / 2));
                }

                if (xOffset != 0)
                {
                    int scrollX = (HorizontalScroll.Value + xOffset);
					HorizontalScroll.Value = Validate(scrollX, HorizontalScroll);
                }
            }

            if (VerticalScroll.Visible)
            {
                int yOffset = 0;

                if (itemRect.Top < ClientRectangle.Top)
                {
                    yOffset = (itemRect.Top - ClientRectangle.Top - (ItemVertSpacing / 2));
                }
                else if (itemRect.Bottom > ClientRectangle.Bottom)
                {
                    yOffset = (itemRect.Bottom - ClientRectangle.Bottom + (ItemVertSpacing / 2));
                }

                if (yOffset != 0)
                {
                    int scrollY = (VerticalScroll.Value + yOffset);
                    VerticalScroll.Value = Validate(scrollY, VerticalScroll);
                }
            }

			PerformLayout();
            Invalidate();
        }

		protected void EnableSelectionNotifications(bool enable)
		{
			// TODO
		}

		protected bool HandleCursorKey(Keys key)
		{
			NetworkItem selItem = SelectedItem;

			if (selItem == null)
				return false;

			uint nextItemId = 0;

			switch (key)
			{
			case Keys.Left:
				if (selItem.Position.X > 0)
				{
					// Try for an item at the same vertical position
					var leftItems = Data.Items.GetHorizontalItems(selItem.Position.Y, 0, selItem.Position.X - 1);

					// else try for the first of this item's dependencies
					if (leftItems.Count == 0)
						leftItems = Data.Items.GetItemDependencies(selItem);

					if (leftItems.Count > 0)
						nextItemId = leftItems[leftItems.Count - 1].UniqueId;
				}
				break;

			case Keys.Right:
				{
					// Try for an item at the same vertical position
					var rightItems = Data.Items.GetHorizontalItems(selItem.Position.Y, selItem.Position.X + 1);

					// else try for the first of this item's dependents
					if (rightItems.Count == 0)
						rightItems = Data.Items.GetItemDependents(selItem);

					if (rightItems.Count > 0)
						nextItemId = rightItems[0].UniqueId;
				}
				break;

			case Keys.Up:
				if (selItem.Position.Y > 0)
				{
					// Try for an item at the same horizontal position
					var upperItems = Data.Items.GetVerticalItems(selItem.Position.X, 0, selItem.Position.Y - 1);

					if (upperItems.Count > 0)
						nextItemId = upperItems[upperItems.Count - 1].UniqueId;
				}
				break;

			case Keys.Down:
				{
					// Try for an item at the same horizontal position
					var lowerItems = Data.Items.GetVerticalItems(selItem.Position.X, selItem.Position.Y + 1);

					if (lowerItems.Count > 0)
						nextItemId = lowerItems[0].UniqueId;
				}
				break;
			}

			if (nextItemId != 0)
			{
				SelectedItemId = nextItemId;

				EnsureItemVisible(SelectedItem);
				Invalidate();

				return true;
			}

			return false;
		}

		protected virtual int GetMinItemHeight()
		{
			return 10;
		}

		protected bool IsEmpty()
		{
			return true;
		}

		protected NetworkItem SelectedItem
		{
			get
			{
				return Data.GetItem(SelectedItemId);
			}
		}

		protected Font ScaledFont(Font font)
		{
			if ((font != null) && (ZoomFactor < 1.0))
				return new Font(font.FontFamily, font.Size * ZoomFactor, font.Style);

			// else
			return font;
		}

        protected NetworkItem HitTestPositions(Point point)
        {
            // TODO

            return null;
        }

    }

}