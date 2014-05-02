using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Bicikelj.Controls
{
	public interface IProportional
	{
		double GetProportion();
	}

	public class ProportionalPanel : Panel
	{
		public Orientation Orientation
		{
			get
			{
				return (Orientation)GetValue(OrientationProperty);
			}
			set
			{
				SetValue(OrientationProperty, value);
			}
		}

		// Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ProportionalPanel), new PropertyMetadata(System.Windows.Controls.Orientation.Vertical));

		protected override Size ArrangeOverride(Size finalSize)
		{
			double offset = 0;
			double totalProportion = 0;
			foreach (var c in Children.OfType<FrameworkElement>())
			{

				if (c.DataContext is IProportional)
				{
					totalProportion += (c.DataContext as IProportional).GetProportion();
				}
				else
				{
					totalProportion += Orientation == Orientation.Vertical
											? c.DesiredSize.Height
											: c.DesiredSize.Width;
				}
			}
			if (Orientation == Orientation.Vertical)
			{
				foreach (var c in Children.OfType<FrameworkElement>())
				{
					double p = 0;
					if (c.DataContext is IProportional)
					{
						p = (c.DataContext as IProportional).GetProportion();
					}
					else
					{
						p = c.DesiredSize.Height;
					}
					double d = Math.Max(0, Math.Floor((finalSize.Height * p) / totalProportion));
					c.Arrange(new Rect(0, offset, finalSize.Width, d));
					offset += d;
				}
			}
			else
			{
				foreach (var c in Children.OfType<FrameworkElement>())
				{
					double p = 0;
					if (c.DataContext is IProportional)
					{
						p = (c.DataContext as IProportional).GetProportion();
					}
					else
					{
						p = c.DesiredSize.Width;
					}
					double d = Math.Max(0, Math.Floor((finalSize.Width * p) / totalProportion));
					c.Arrange(new Rect(offset, 0, d, finalSize.Height));
					offset += d;
				}

			}
			return finalSize;
		}
		protected override Size MeasureOverride(Size availableSize)
		{
			Size finalSize = new Size(availableSize.Width, availableSize.Height);
			double totalProportion = 0;
			foreach (var c in Children.OfType<FrameworkElement>())
			{

				if (c.DataContext is IProportional)
				{
					totalProportion += (c.DataContext as IProportional).GetProportion();
				}
				else
				{
					totalProportion += Orientation == Orientation.Vertical
											? c.DesiredSize.Height
											: c.DesiredSize.Width;
				}
			}

			double sizeAvailable, maxAlternate = 0;
			switch (Orientation)
			{
				case Orientation.Horizontal:
					sizeAvailable = availableSize.Width;
					if (double.IsNaN(sizeAvailable) || double.IsPositiveInfinity(sizeAvailable))
					{
						sizeAvailable = 0;
						foreach (var c in Children)
						{
							c.Measure(availableSize);
							sizeAvailable += c.DesiredSize.Width;
							maxAlternate = Math.Max(maxAlternate, c.DesiredSize.Height);
						}
						finalSize.Width = sizeAvailable;
						finalSize.Height = maxAlternate;

					}
					else
					{
						foreach (var c in Children.OfType<FrameworkElement>())
						{
							double p;
							if (c.DataContext is IProportional)
							{
								p = (c.DataContext as IProportional).GetProportion();
							}
							else
							{
								p = Orientation == Orientation.Vertical
														? c.DesiredSize.Height
														: c.DesiredSize.Width;
							}
							c.Measure(new Size(Math.Max(0, Math.Floor((sizeAvailable * p) / totalProportion)), finalSize.Height));
						}
					}
					break;
				case Orientation.Vertical:
					sizeAvailable = availableSize.Height;
					if (double.IsNaN(sizeAvailable) || double.IsPositiveInfinity(sizeAvailable))
					{
						sizeAvailable = 0;
						foreach (var c in Children)
						{
							c.Measure(availableSize);
							sizeAvailable += c.DesiredSize.Height;
							maxAlternate = Math.Max(maxAlternate, c.DesiredSize.Width);
						}
						finalSize.Height = sizeAvailable;
						finalSize.Width = maxAlternate;
					}
					else
					{
						foreach (var c in Children.OfType<FrameworkElement>())
						{
							double p;
							if (c.DataContext is IProportional)
							{
								p = (c.DataContext as IProportional).GetProportion();
							}
							else
							{
								p = Orientation == Orientation.Vertical
														? c.DesiredSize.Height
														: c.DesiredSize.Width;
							}
							c.Measure(new Size(finalSize.Width, Math.Max(0, Math.Floor((sizeAvailable * p) / totalProportion))));
						}
					}

					break;
			}
			return finalSize;
		}
	}
}
