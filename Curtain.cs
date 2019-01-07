using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MusicBox
{
	public static class Curtain
	{
		class CurtainScope : IDisposable
		{
			Grid _container;
			UIElement _curtain;

			public CurtainScope(Grid container)
			{
				_container = container;
				_curtain = CreateCurtain(_container);
			}

			public void Dispose()
			{
				if (_curtain != null)
				{
					RemoveCurtain(_container, _curtain);
					_curtain = null;
				}
			}
		}

		public static IDisposable ShowCurtain(Grid container)
		{
			return new CurtainScope(container);
		}

		static UIElement CreateCurtain(Grid container)
		{
			var curtain = new Rectangle();

			curtain.HorizontalAlignment = HorizontalAlignment.Stretch;
			curtain.VerticalAlignment = VerticalAlignment.Stretch;

			curtain.Fill = Brushes.Black;
			curtain.Opacity = 0.4;

			if (container.ColumnDefinitions.Count > 1)
				Grid.SetColumnSpan(curtain, container.ColumnDefinitions.Count);
			if (container.RowDefinitions.Count > 1)
				Grid.SetRowSpan(curtain, container.RowDefinitions.Count);

			container.Children.Add(curtain);

			return curtain;
		}

		static void RemoveCurtain(Grid container, UIElement curtain)
		{
			container.Children.Remove(curtain);
		}
	}
}
