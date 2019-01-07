using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

// Based on: https://stackoverflow.com/questions/833943/watermark-hint-text-placeholder-textbox
namespace MusicBox
{
	/// <summary>
	/// Class that provides the Placeholder attached property
	/// </summary>
	public static class PlaceholderService
	{
		/// <summary>
		/// Placeholder Attached Dependency Property
		/// </summary>
		public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached("Placeholder", typeof(object), typeof(PlaceholderService), new FrameworkPropertyMetadata((object)null, new PropertyChangedCallback(OnPlaceholderChanged)));

		#region Private Fields

		/// <summary>
		/// Dictionary of ItemsControls
		/// </summary>
		private static readonly Dictionary<object, ItemsControl> itemsControls = new Dictionary<object, ItemsControl>();

		#endregion

		/// <summary>
		/// Gets the Placeholder property.  This dependency property indicates the placeholder for the control.
		/// </summary>
		/// <param name="d"><see cref="DependencyObject" /> to get the property from</param>
		/// <returns>The value of the Placeholder property</returns>
		public static object GetPlaceholder(DependencyObject d)
		{
			return (object)d.GetValue(PlaceholderProperty);
		}

		/// <summary>
		/// Sets the Placeholder property.  This dependency property indicates the placeholder for the control.
		/// </summary>
		/// <param name="d"><see cref="DependencyObject" /> to set the property on</param>
		/// <param name="value">value of the property</param>
		public static void SetPlaceholder(DependencyObject d, object value)
		{
			d.SetValue(PlaceholderProperty, value);
		}

		/// <summary>
		/// Handles changes to the Placeholder property.
		/// </summary>
		/// <param name="d"><see cref="DependencyObject" /> that fired the event</param>
		/// <param name="e">A <see cref="DependencyPropertyChangedEventArgs" /> that contains the event data.</param>
		private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Control control = (Control)d;

			control.Loaded += Control_Loaded;

			if (d is ComboBox comboBox)
			{
				comboBox.GotKeyboardFocus += Control_GotKeyboardFocus;
				comboBox.LostKeyboardFocus += Control_Loaded;
			}
			else if (d is TextBox textBox)
			{
				textBox.GotKeyboardFocus += Control_GotKeyboardFocus;
				textBox.LostKeyboardFocus += Control_Loaded;
				textBox.TextChanged += Control_GotKeyboardFocus;
			}

			if ((d is ItemsControl i) && !(i is ComboBox))
			{
				// For Items property
				i.ItemContainerGenerator.ItemsChanged += ItemsChanged;
				itemsControls.Add(i.ItemContainerGenerator, i);

				// For ItemsSource property
				DependencyPropertyDescriptor prop = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, i.GetType());
				prop.AddValueChanged(i, ItemsSourceChanged);
			}
		}

		#region Event Handlers

		/// <summary>
		/// Handle the GotFocus event on the control
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">A <see cref="RoutedEventArgs" /> that contains the event data.</param>
		private static void Control_GotKeyboardFocus(object sender, RoutedEventArgs e)
		{
			Control c = (Control)sender;

			if (ShouldShowPlaceholder(c))
				ShowPlaceholder(c);
			else
				RemovePlaceholder(c);
		}

		/// <summary>
		/// Handle the Loaded and LostFocus event on the control
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">A <see cref="RoutedEventArgs" /> that contains the event data.</param>
		private static void Control_Loaded(object sender, RoutedEventArgs e)
		{
			Control control = (Control)sender;

			if (ShouldShowPlaceholder(control))
				ShowPlaceholder(control);
		}

		/// <summary>
		/// Event handler for the items source changed event
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">A <see cref="EventArgs" /> that contains the event data.</param>
		private static void ItemsSourceChanged(object sender, EventArgs e)
		{
			ItemsControl c = (ItemsControl)sender;

			if (c.ItemsSource != null)
			{
				if (ShouldShowPlaceholder(c))
					ShowPlaceholder(c);
				else
					RemovePlaceholder(c);
			}
			else
				ShowPlaceholder(c);
		}

		/// <summary>
		/// Event handler for the items changed event
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">A <see cref="ItemsChangedEventArgs" /> that contains the event data.</param>
		private static void ItemsChanged(object sender, ItemsChangedEventArgs e)
		{
			if (itemsControls.TryGetValue(sender, out var control))
			{
				if (ShouldShowPlaceholder(control))
					ShowPlaceholder(control);
				else
					RemovePlaceholder(control);
			}
		}

		#endregion

		#region Helper Methods

		/// <summary>
		/// Remove the placeholder from the specified element
		/// </summary>
		/// <param name="control">Element from which to remove the placeholder</param>
		private static void RemovePlaceholder(UIElement control)
		{
			AdornerLayer layer = AdornerLayer.GetAdornerLayer(control);

			// layer could be null if control is no longer in the visual tree
			if (layer != null)
			{
				Adorner[] adorners = layer.GetAdorners(control);

				if (adorners == null)
					return;

				foreach (Adorner adorner in adorners)
				{
					if (adorner is PlaceholderAdorner)
					{
						adorner.Visibility = Visibility.Hidden;
						layer.Remove(adorner);
					}
				}
			}
		}

		/// <summary>
		/// Show the placeholder on the specified control
		/// </summary>
		/// <param name="control">Control on which to show the placeholder</param>
		private static void ShowPlaceholder(Control control)
		{
			AdornerLayer layer = AdornerLayer.GetAdornerLayer(control);

			// layer could be null if control is no longer in the visual tree
			if (layer != null)
			{
				layer.Add(new PlaceholderAdorner(control, GetPlaceholder(control)));
			}
		}

		/// <summary>
		/// Indicates whether or not the placeholder should be shown on the specified control
		/// </summary>
		/// <param name="c"><see cref="Control" /> to test</param>
		/// <returns>true if the placeholder should be shown; false otherwise</returns>
		private static bool ShouldShowPlaceholder(Control c)
		{
			if (c is ComboBox comboBox)
				return comboBox.Text == string.Empty;
			if (c is TextBox textBox)
				return textBox.Text == string.Empty;
			if (c is ItemsControl itemsControl)
				return itemsControl.Items.Count == 0;

			return false;
		}

		#endregion
	}
}