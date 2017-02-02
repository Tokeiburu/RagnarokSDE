using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Database;
using SDE.Editor.Generic.Core;
using TokeiLibrary;

namespace SDE.Editor.Generic.TabsMakerCore {
	public static class DisplayablePropertyHelper {
		public static List<UIElement> GetAll(UIElement element, DbAttribute attribute) {
			return GetAll(element, attribute.DisplayName);
		}

		public static List<UIElement> GetAll(UIElement element, string display) {
			List<UIElement> elements = new List<UIElement>();

			var sp = element as StackPanel;
			if (sp != null) {
				foreach (UIElement selement in sp.Children) {
					if (selement is Grid || selement is StackPanel)
						elements.AddRange(GetAll(selement, display));
				}
			}

			var g = element as Grid;
			if (g != null) {
				foreach (UIElement selement in g.Children) {
					var label = selement as Label;
					if (label != null) {
						// We found what we were looking for...!
						if (label.Content.ToString() == display) {
							// Retrieve the row and column
							int row = (int)label.GetValue(Grid.RowProperty);
							int col = (int)label.GetValue(Grid.ColumnProperty) + 1;
							var selements = g.Children.Cast<UIElement>().Where(p => (int)p.GetValue(Grid.RowProperty) == row && (int)p.GetValue(Grid.ColumnProperty) == col).ToList();

							elements.AddRange(selements);
							elements.Add(label);
						}
					}
					else {
						elements.AddRange(GetAll(selement, display));
					}
				}
			}

			return elements;
		}

		public static List<UIElement> GetAll<TKey, TValue>(UIElement element, DbAttribute attribute, DisplayableProperty<TKey, TValue> dp) where TValue : Tuple {
			if (dp == null) return GetAll(element, attribute);

			var elements = GetAll(element, attribute);

			if (elements.Count == 0) {
				// Search in the undeployed attributes
				Label label = dp.GetLabel(attribute.DisplayName);

				if (label == null) return elements;

				// Only items from the primary grid can be returned
				return dp.GetComponents((int)label.GetValue(Grid.RowProperty), (int)label.GetValue(Grid.ColumnProperty) + 1).Where(p => p.Parent == label.Parent).OfType<UIElement>().ToList();
			}

			return elements;
		}

		public static List<T> Find<T>(UIElement element, DbAttribute attribute) where T : FrameworkElement {
			return Find<int, ReadableTuple<int>, T>(element, attribute, null);
		}

		public static List<T> Find<TKey, TValue, T>(UIElement element, DbAttribute attribute, DisplayableProperty<TKey, TValue> dp) where TValue : Tuple where T : FrameworkElement {
			var elements = GetAll(element, attribute, dp);

			if (elements.Count == 0) return null;

			List<UIElement> allElements = new List<UIElement>();

			while (elements.Any(p => p is Panel)) {
				foreach (var telement in elements) {
					if (telement is Panel)
						allElements.AddRange(((Panel)telement).Children.OfType<UIElement>());
					else
						allElements.Add(telement);
				}

				elements = allElements;
			}

			return elements.OfType<T>().ToList();
		}

		public static List<T> FindAll<T>(UIElement element)
			where T : FrameworkElement {
			List<UIElement> allElements = new List<UIElement>();
			List<UIElement> elements = new List<UIElement> { element };

			while (elements.Any(p => p is Panel)) {
				allElements.Clear();

				for (int index = 0; index < elements.Count; index++) {
					var telement = elements[index];

					if (telement is Panel)
						allElements.AddRange(((Panel)telement).Children.OfType<UIElement>());
					else
						allElements.Add(telement);
				}

				elements = new List<UIElement>(allElements);
			}

			return elements.OfType<T>().ToList();
		}

		public static void CheckAttributeRestrictions<TKey>(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, GTabSettings<TKey, ReadableTuple<TKey>> settings, BaseDb gdb) {
			foreach (var attributeS in settings.AttributeList.Attributes) {
				DbAttribute attribute = attributeS;

				if (attribute.Requirements.Renewal == RenewalType.Both && attribute.Requirements.Server == ServerType.Both) {
					continue;
				}

				tab.PropertiesGrid.Dispatch(delegate {
					var gridAttributes = GetAll(tab.PropertiesGrid, attribute);
					RenewalType rType = DbPathLocator.GetIsRenewal() ? RenewalType.Renewal : RenewalType.PreRenewal;
					ServerType sType = DbPathLocator.GetServerType();

					gridAttributes.ForEach(p => p.IsEnabled = false);

					if ((attribute.Requirements.Renewal & rType) == rType && (attribute.Requirements.Server & sType) == sType) {
						gridAttributes.ForEach(p => p.IsEnabled = true);
					}
					else {
						gridAttributes.ForEach(p => p.IsEnabled = false);
					}
				});
			}
		}
	}
}