using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Database;
using GRF.Threading;
using SDE.Editor.Engines.DatabaseEngine;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Extension;

namespace SDE.Editor.Generic.TabsMakerCore {
	public partial class GSearchEngine<TKey, TValue> where TValue : Database.Tuple {
		private bool _filterEnabled = true;
		private bool _ignoreFilter;

		public void Filter(object sender) {
			_filter(sender, null);
		}

		public void Filter(object sender, Action finished) {
			_filter(sender, finished);
		}

		public void ClearFilter() {
			try {
				_filterEnabled = false;
				_itemsSearchSettings.InternalClear();

				foreach (var attribute in _states) {
					_itemsSearchSettings[attribute.Key] = attribute.Value;
				}

				if (_tbItemsRange != null)
					_tbItemsRange.Dispatch(p => p.Clear());

				if (_tbSearchItems != null)
					_tbSearchItems.Dispatch(p => p.Clear());
			}
			finally {
				_filterEnabled = true;
			}
		}

		public void IgnoreFilterOnce() {
			_ignoreFilter = true;
		}

		private void _filter(object sender) {
			_filter(sender, null);
		}

		private void _filter(object sender, Action finished) {
			if (!_filterEnabled) return;

			string currentSearch = _searchItemsFilter;
			IsFiltering = true;
			_validateLoaded();
			GrfThread.Start(() => _filterInternal(currentSearch, finished), "SDEditor - Search filter items thread");
		}

		private void _filterInternal(string currentSearch, Action finished) {
			lock (_filterLock) {
				IsFiltering = true;
				bool isCondition = false;

				try {
					if (currentSearch != _searchItemsFilter) return;
					if (_items == null) return;

					List<TValue> allItems;

					if (SubsetCondition != null) {
						allItems = _getItemsFunction().Where(p => SubsetCondition(p)).ToList();
					}
					else {
						allItems = _getItemsFunction();
					}

					if (SetupImageDataGetter != null) {
						allItems.Where(p => p.GetImageData == null).ToList().ForEach(p => SetupImageDataGetter(p));
					}

					if (allItems.Count == 0) {
						_items.Dispatch(r => r.ItemsSource = new RangeObservableCollection<TValue>(new List<TValue>()));
						_textBoxOk();
						OnFilterFinished(new List<TValue>());
						return;
					}

					if (!_searchFirstTimeSet) {
						_entryComparer = new DatabaseItemSorter<TValue>(_settings.AttributeList);
						_entryComparer.SetSort(_settings.AttId.AttributeName, ListSortDirection.Ascending);
						_searchFirstTimeSet = true;
					}

					_items.Dispatch(p => _entryComparer.SetSort(WpfUtils.GetLastGetSearchAccessor(_items), WpfUtils.GetLastSortDirection(_items)));

					if (_ignoreFilter) {
						allItems = allItems.OrderBy(p => p, _entryComparer).ToList();
						_items.Dispatch(r => r.ItemsSource = new RangeObservableCollection<TValue>(allItems));
						OnFilterFinished(allItems);
						_ignoreFilter = false;
						return;
					}

					Condition condition;
					List<string> search = _getSearch(currentSearch, out condition);

					isCondition = search == null;

					if (search != null && search.Count == 0 &&
					    !_attributes.Where(p => p.DataType.BaseType == typeof(Enum)).Any(p => _itemsSearchSettings[p]) &&
					    !_itemsSearchSettings[GSearchSettings.TupleAdded] &&
					    !_itemsSearchSettings[GSearchSettings.TupleModified] &&
					    !_itemsSearchSettings[GSearchSettings.TupleRange]) {
						allItems = allItems.OrderBy(p => p, _entryComparer).ToList();
						_items.Dispatch(r => r.ItemsSource = new RangeObservableCollection<TValue>(allItems));
						_textBoxOk();
						OnFilterFinished(allItems);
						return;
					}

					if (currentSearch != _searchItemsFilter) return;

					_textBoxProcessing();

					string predicateSearch = _tbItemsRange.Dispatch(() => _tbItemsRange.Text);
					bool isWiden = _itemsSearchSettings.Get(GSearchSettings.Mode) == "0";

					List<Func<TValue, string, bool>> generalPredicates =
						(from attribute in _attributes
							where attribute != null
							let attributeCopy = attribute
							where _itemsSearchSettings[attributeCopy]
							where attribute.DataType.BaseType != typeof(Enum)
							select new Func<TValue, string, bool>((p, s) => p.GetValue<string>(attributeCopy).IndexOf(s, StringComparison.OrdinalIgnoreCase) != -1)).ToList();

					bool isAttributeRestricted = false;

					if (search == null) {
						_textBoxPrediate();
						generalPredicates.Clear();
						var predicate = condition.ToPredicate(_settings);
						generalPredicates = new List<Func<TValue, string, bool>> { predicate };
					}
					else if (search.Any(p => p.StartsWith("[", StringComparison.Ordinal) && p.EndsWith("]", StringComparison.Ordinal))) {
						generalPredicates.Clear();

						for (int index = 0; index < search.Count; index++) {
							string se = search[index];
							int ival;

							if (se.StartsWith("[", StringComparison.Ordinal) && se.EndsWith("]", StringComparison.Ordinal)) {
								se = se.Substring(1, se.Length - 2);
								se = se.Replace("_", " ");
								var att = _settings.AttributeList.Attributes.FirstOrDefault(p => p.DisplayName.IndexOf(se, 0, StringComparison.OrdinalIgnoreCase) > -1);

								if (Int32.TryParse(se, out ival) || att != null) {
									if (ival < _settings.AttributeList.Attributes.Count) {
										DbAttribute attribute = att ?? _settings.AttributeList.Attributes[ival];
										isAttributeRestricted = true;
										//generalPredicates.Add(new Func<TValue, string, bool>((p, s) => p.GetValue<string>(attribute).IndexOf(s, StringComparison.OrdinalIgnoreCase) != -1));
										string nextSearch = index + 1 < search.Count ? search[index + 1] : "";
										generalPredicates.Add(new Func<TValue, string, bool>((p, s) => String.Compare(p.GetValue<string>(attribute), nextSearch, StringComparison.OrdinalIgnoreCase) == 0));
									}
									search.RemoveAt(index);
									index--;
								}
							}
						}
					}

					List<DbAttribute> enumAttributes = _attributes.Where(p => p.DataType.BaseType == typeof(Enum) && _itemsSearchSettings[p]).ToList();

					bool hasTuplePredicates = _itemsSearchSettings[GSearchSettings.TupleAdded] ||
					                          _itemsSearchSettings[GSearchSettings.TupleModified] ||
					                          _itemsSearchSettings[GSearchSettings.TupleRange] ||
					                          enumAttributes.Any();

					Func<TValue, bool> tuplePredicate = null;

					if (hasTuplePredicates) tuplePredicate = _getTuplePredicates(enumAttributes, predicateSearch);
					if (currentSearch != _searchItemsFilter) return;

					List<TValue> result = _getResults(search, isAttributeRestricted, generalPredicates, allItems, tuplePredicate, isWiden);

					if (currentSearch != _searchItemsFilter) {
						_textBoxOk();
						return;
					}

					_items.Dispatch(r => r.ItemsSource = new RangeObservableCollection<TValue>(result));

					if (!isCondition)
						_textBoxOk();

					OnFilterFinished(result);
				}
				catch {
					_textBoxOk();
				}
				finally {
					try {
						if (finished != null)
							finished();
					}
					catch {
					}
					IsFiltering = false;
				}
			}
		}

		private void _textBoxOk() {
			_tbSearchItems.Dispatch(delegate {
				_tbSearchItems.Background = Application.Current.Resources["GSearchEngineOk"] as Brush;
			});
		}

		private void _textBoxProcessing() {
			_tbSearchItems.Dispatch(delegate {
				_tbSearchItems.Background = Application.Current.Resources["GSearchEngineProcessing"] as Brush;
			});
		}

		private void _textBoxPrediate() {
			_tbSearchItems.Dispatch(delegate {
				_tbSearchItems.Background = Application.Current.Resources["GSearchEnginePredicate"] as Brush;
			});
		}

		private List<TValue> _getResults(ICollection<string> search, bool isAttributeRestricted, ICollection<Func<TValue, string, bool>> generalPredicates, IEnumerable<TValue> allItems, Func<TValue, bool> tuplePredicate, bool isWiden) {
			if (search == null)
				search = new List<string> { "" };

			if (isAttributeRestricted && generalPredicates.Count != 0) {
				if (tuplePredicate == null) {
					if (isWiden)
						return allItems.Where(item => generalPredicates.Any(predicate => predicate(item, null))).OrderBy(p => p, _entryComparer).ToList();
					return allItems.Where(item => generalPredicates.All(predicate => predicate(item, null))).OrderBy(p => p, _entryComparer).ToList();
				}
				if (isWiden)
					return allItems.Where(tuplePredicate).Where(item => generalPredicates.Any(predicate => predicate(item, null))).OrderBy(p => p, _entryComparer).ToList();
				return allItems.Where(tuplePredicate).Where(item => generalPredicates.All(predicate => predicate(item, null))).OrderBy(p => p, _entryComparer).ToList();
			}

			if (generalPredicates.Count == 0) {
				if (tuplePredicate == null)
					return allItems.OrderBy(p => p, _entryComparer).ToList();
				return allItems.Where(tuplePredicate).OrderBy(p => p, _entryComparer).ToList();
			}

			if (tuplePredicate == null) {
				if (isWiden)
					return allItems.Where(item => search.Any(searchWord => generalPredicates.Any(predicate => predicate(item, searchWord)))).OrderBy(p => p, _entryComparer).ToList();
				return allItems.Where(item => search.All(searchWord => generalPredicates.Any(predicate => predicate(item, searchWord)))).OrderBy(p => p, _entryComparer).ToList();
			}
			bool isSearchEmpty = search.Count == 0;

			if (isWiden)
				return (isSearchEmpty ? allItems.Where(tuplePredicate).OrderBy(p => p, _entryComparer) : allItems.Where(tuplePredicate).Where(item => search.Any(searchWord => generalPredicates.Any(predicate => predicate(item, searchWord))))).OrderBy(p => p, _entryComparer).ToList();
			return (isSearchEmpty ? allItems.Where(tuplePredicate).OrderBy(p => p, _entryComparer) : allItems.Where(tuplePredicate).Where(item => search.All(searchWord => generalPredicates.Any(predicate => predicate(item, searchWord))))).OrderBy(p => p, _entryComparer).ToList();
		}

		private Func<TValue, bool> _getTuplePredicates(IEnumerable<DbAttribute> enumAttributes, string predicateSearch) {
			Func<TValue, bool> tuplePredicate;

			List<Func<TValue, bool>> tuplePredicates = new List<Func<TValue, bool>>();

			if (_itemsSearchSettings[GSearchSettings.TupleAdded])
				tuplePredicates.Add(new Func<TValue, bool>(item => item.Added));

			if (_itemsSearchSettings[GSearchSettings.TupleModified])
				tuplePredicates.Add(new Func<TValue, bool>(item => item.Modified));

			List<Func<TValue, bool>> tupleTypePredicates =
				(from attributeCopy in enumAttributes
					let val = (string)attributeCopy.AttachedAttribute
					select new Func<TValue, bool>(item => item.GetValue<string>(attributeCopy) == val)).ToList();

			List<Func<TValue, bool>> tupleRangePredicates = new List<Func<TValue, bool>>();

			if (_itemsSearchSettings[GSearchSettings.TupleRange])
				tupleRangePredicates = GetRangePredicates(predicateSearch);

			if (tupleTypePredicates.Count > 0) {
				if (_itemsSearchSettings[GSearchSettings.TupleRange] && tuplePredicates.Count > 0)
					tuplePredicate = new Func<TValue, bool>(item => tupleRangePredicates.Any(q => q(item)) && tuplePredicates.Any(q => q(item)) && tupleTypePredicates.All(q => q(item)));
				else if (_itemsSearchSettings[GSearchSettings.TupleRange])
					tuplePredicate = new Func<TValue, bool>(item => tupleRangePredicates.Any(q => q(item)) && tupleTypePredicates.All(q => q(item)));
				else if (tuplePredicates.Count == 0)
					tuplePredicate = new Func<TValue, bool>(item => tupleTypePredicates.All(q => q(item)));
				else
					tuplePredicate = new Func<TValue, bool>(item => tuplePredicates.Any(q => q(item)) && tupleTypePredicates.All(q => q(item)));
			}
			else {
				if (_itemsSearchSettings[GSearchSettings.TupleRange] && tuplePredicates.Count > 0)
					tuplePredicate = new Func<TValue, bool>(item => tupleRangePredicates.Any(q => q(item)) && tuplePredicates.Any(q => q(item)));
				else if (_itemsSearchSettings[GSearchSettings.TupleRange])
					tuplePredicate = new Func<TValue, bool>(item => tupleRangePredicates.Any(q => q(item)));
				else
					tuplePredicate = new Func<TValue, bool>(item => tuplePredicates.Any(q => q(item)));
			}

			return tuplePredicate;
		}

		private readonly string[] _symbols = { " <= ", " < ", " > ", " >= ", " = ", " == ", " != ", " ~= ", "!( ", " not(", " & ", " | ", " << ", " >> ", " % ", " * ", " / ", " ^ ", " - ", " contains ", " exclude ", " ⊃ ", " ⊅ " };

		private List<string> _getSearch(string currentSearch, out Condition condition) {
			condition = null;

			if (_symbols.Any(currentSearch.Contains)) {
				// Parse the expression is a condition
				try {
					var currentSearch2 = currentSearch
						.Replace(" and ", " && ")
						.Replace(" or ", " || ")
						.Replace(" != ", " ~= ")
						.Replace(" = ", " == ")
						.Replace(" contains ", " ⊃ ")
						.Replace(" exclude ", " ⊅ ")
						.Replace("!(", "not(");

					condition = ConditionLogic.GetCondition(currentSearch2);
					return null;
				}
				catch {
				}
			}

			List<string> search = new List<string>();
			string tempSearch = currentSearch;

			if (tempSearch.Contains('\"')) {
				int indexStart;
				int indexEnd;
				while (true) {
					indexStart = tempSearch.IndexOf('\"');

					if (indexStart < 0)
						break;

					indexEnd = tempSearch.IndexOf('\"', indexStart + 1);

					if (indexEnd < 0)
						break;

					if (indexStart + 1 == indexEnd)
						break;

					search.Add(tempSearch.Substring(indexStart + 1, indexEnd - indexStart - 1));
					tempSearch = tempSearch.Substring(0, indexStart) + tempSearch.Substring(indexEnd + 1, tempSearch.Length - indexEnd - 1);
				}
			}

			search.AddRange(tempSearch.ReplaceAll("  ", " ").Replace("\"", "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList());
			return search;
		}
	}
}