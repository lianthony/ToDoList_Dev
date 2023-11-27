﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace EvidenceBoardUIExtension
{
	public class NodeControlBackgroundImage
	{
		public Image Image { get; private set; }
		public string FilePath { get; private set; } = string.Empty;
		public Rectangle Bounds { get; private set; } = Rectangle.Empty;

		public bool HasImage { get { return (Image != null); } }

		public bool Set(string filePath, Rectangle rect)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				return Clear();

			if (filePath == FilePath)
				return false;

			var image = Image.FromFile(filePath);

			if (image == null)
				return false;

			Image = image;

			FilePath = filePath;
			Bounds = rect;

			return true;
		}

		public bool Clear()
		{
			if (Image == null)
				return false;

			Image = null;
			FilePath = string.Empty;
			Bounds = Rectangle.Empty;

			return true;
		}

		private float AspectRatio { get { return Geometry2D.GetAspectRatio(Image.Size); } }

		public Size CalcSizeToFit(Size size)
		{
			if (!HasImage)
				return Size.Empty;

			float imageAspect = AspectRatio;
			float extentAspect = Geometry2D.GetAspectRatio(size);

			int newWidth = size.Width;

			if (imageAspect < extentAspect)
			{
				// Extents height is the limiting factor
				newWidth = (int)(size.Height * imageAspect);
			}

			return new Size(newWidth, (int)(newWidth / imageAspect));
		}

		public void ResizeToFit(Rectangle extents)
		{
			Rectangle rect = Rectangle.Empty;

			if (HasImage)
			{
				var size = CalcSizeToFit(extents.Size);
				rect = Geometry2D.GetCentredRect(Geometry2D.Centroid(extents), size.Width, size.Height);
			}

			Bounds = rect;
		}
		
		public DragMode HitTest(Point point, int edgeWidth)
		{
			var outerRect = Rectangle.Inflate(Bounds, (edgeWidth / 2), (edgeWidth / 2));

			if (!outerRect.Contains(point))
				return DragMode.None;

			var innerRect = Rectangle.Inflate(Bounds, -(edgeWidth / 2), -edgeWidth / 2);

			if (innerRect.Contains(point))
				return DragMode.Background;

			if ((point.Y > innerRect.Top) && (point.Y < innerRect.Bottom))
			{
				if (point.X <= innerRect.Left)
					return DragMode.BackgroundLeft;

				// else
				return DragMode.BackgroundRight;
			}
			else if ((point.X > innerRect.Left) && (point.X < innerRect.Right))
			{
				if (point.Y <= innerRect.Top)
					return DragMode.BackgroundTop;

				// else
				return DragMode.BackgroundBottom;
			}

			// else
			return DragMode.None;
		}

		public bool SetPosition(Point newCentre)
		{
			if (HasImage)
				return false;

			var centre = Geometry2D.Centroid(Bounds);

			if (newCentre == centre)
				return false;

			Bounds = Geometry2D.GetCentredRect(newCentre, Bounds.Width, Bounds.Height);
			return true;
		}

		public bool InflateWidth(int amount, Size minSize)
		{
			if (!HasImage || (minSize.Width <= 0))
				return false;

			if ((Bounds.Width + amount) < minSize.Width)
				amount = (minSize.Width - Bounds.Width);

			if (amount == 0)
				return false;

			// Calc new width and height preserving aspect ratio
			int newWidth = (Bounds.Width + amount);
			int newHeight = (int)(newWidth / AspectRatio);

			Bounds = Geometry2D.GetCentredRect(Geometry2D.Centroid(Bounds), newWidth, newHeight);
			return true;
		}

		public bool InflateHeight(int amount, Size minSize)
		{
			if (!HasImage || (minSize.Height <= 0))
				return false;

			if (amount < 0)
				amount = Math.Max(minSize.Height, (Bounds.Height + amount)) - Bounds.Height;

			if (amount == 0)
				return false;

			// Calc new width and height preserving aspect ratio
			int newHeight = (Bounds.Height + amount);
			int newWidth = (int)(newHeight * AspectRatio);

			Bounds = Geometry2D.GetCentredRect(Geometry2D.Centroid(Bounds), newWidth, newHeight);
			return true;
		}

		public Cursor GetCursor(DragMode edge)
		{
			switch (edge)
			{
			case DragMode.Background:
				return Cursors.Arrow;

			case DragMode.BackgroundLeft:
			case DragMode.BackgroundRight:
				return Cursors.SizeWE;

			case DragMode.BackgroundTop:
			case DragMode.BackgroundBottom:
				return Cursors.SizeNS;
			}

			return null;
		}
	}
}
