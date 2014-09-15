using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Database;
using GRF.Threading;
using SDE.Tools.DatabaseEditor.Engines;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	public partial class GSearchEngine<TKey, TValue> where TValue : Tuple {
		public void Filter(object sender) {
			_filter(sender);
		}

		private void _filter(object sender) {
			string currentSearch = _searchItemsFilter;
			IsFiltering = true;
			GrfThread.Start(() => _filterInternal(currentSearch), "CDEditor - Search filter items thread");
		}

		private void _filterInternal(string currentSearch) {
			lock (_filterLock) {
				IsFiltering = true;

				try {
					if (currentSearch != _searchItemsFilter) return;
					if (_items == null) return;

					List<TValue> allItems;

					if (SubsetCondition != null) {
						allItems = _table.FastItems.Where(p => SubsetCondition(p)).ToList();
					}
					else {
						allItems = _table.FastItems;
					}

					if (SetupImageDataGetter != null) {
						allItems.Where(p => p.GetImageData == null).ToList().ForEach(p => SetupImageDataGetter(p));
					}

					List<string> search = _getSearch(currentSearch);

					if (allItems.Count == 0) {
						_items.Dispatch(r => r.ItemsSource = new RangeObservableCollection<TValue>(new List<TValue>()));
						WpfUtilities.TextBoxOk(_tbSearchItems);
						OnFilterFinished(new List<TValue>());
						return;
					}

					WpfUtilities.TextBoxProcessing(_tbSearchItems);

					if (!_searchFirstTimeSet) {
						_entryComparer = new DatabaseItemSorter<TValue>(_settings.AttributeList);
						_entryComparer.SetSort(_settings.AttId.AttributeName, ListSortDirection.Ascending);
						_searchFirstTimeSet = true;
					}

					_items.Dispatch(p => _entryComparer.SetSort(WpfUtils.GetLastGetSearchAccessor(_items), WpfUtils.GetLastSortDirection(_items)));

					if (search.Count == 0 &&
						!_attributes.Where(p => p.DataType.BaseType == typeof(Enum)).Any(p => _itemsSearchSettings[p]) &&
						!_itemsSearchSettings[GSearchSettings.TupleAdded] &&
						!_itemsSearchSettings[GSearchSettings.TupleModified] &&
						!_itemsSearchSettings[GSearchSettings.TupleRange]) {
						allItems = allItems.OrderBy(p => p, _entryComparer).ToList();
						_items.Dispatch(r => r.ItemsSource = new RangeObservableCollection<TValue>(allItems));
						WpfUtilities.TextBoxOk(_tbSearchItems);
						OnFilterFinished(allItems);
						return;
					}

					if (currentSearch != _searchItemsFilter) return;

					string predicateSearch = _tbItemsRange.Dispatch(() => _tbItemsRange.Text);
					bool isWiden = _itemsSearchSettings.Get(GSearchSettings.Mode) == "0";
					
					List<Func<TValue, string, bool>> generalPredicates = 
						(from attribute in _attributes 
						 where attribute != null 
						 let attributeCopy = attribute 
						 where _itemsSearchSettings[attributeCopy] 
						 where attribute.DataType.BaseType != typeof (Enum) 
						 select new Func<TValue, string, bool>((p, s) => p.GetValue<string>(attributeCopy).IndexOf(s, StringComparison.InvariantCultureIgnoreCase) != -1)).ToList();

					List<DbAttribute> enumAttributes = _attributes.Where(p => p.DataType.BaseType == typeof (Enum) && _itemsSearchSettings[p]).ToList();

					bool hasTuplePredicates = _itemsSearchSettings[GSearchSettings.TupleAdded] ||
											  _itemsSearchSettings[GSearchSettings.TupleModified] ||
											  _itemsSearchSettings[GSearchSettings.TupleRange] ||
											  enumAttributes.Any();

					Func<TValue, bool> tuplePredicate = null;

					if (hasTuplePredicates) tuplePredicate = _getTuplePredicates(enumAttributes, predicateSearch);
					if (currentSearch != _searchItemsFilter) return;

					List<TValue> result;

					if (generalPredicates.Count == 0) {
						if (tuplePredicate == null) {
							result = allItems.OrderBy(p => p, _entryComparer).ToList();
						}
						else {
							result = allItems.Where(p => tuplePredicate(p)).OrderBy(p => p, _entryComparer).ToList();
						}
					}
					else {
						if (tuplePredicate == null) {
							if (isWiden)
								result = allItems.Where(item => search.Any(searchWord => generalPredicates.Any(predicate => predicate(item, searchWord)))).OrderBy(p => p, _entryComparer).ToList();
							else
								result = allItems.Where(item => search.All(searchWord => generalPredicates.Any(predicate => predicate(item, searchWord)))).OrderBy(p => p, _entryComparer).ToList();
						}
						else {
							bool isSearchEmpty = search.Count == 0;

							if (isWiden)
								result = (isSearchEmpty ? allItems.Where(p => tuplePredicate(p)).OrderBy(p => p, _entryComparer) : allItems.Where(p => tuplePredicate(p)).Where(item => search.Any(searchWord => generalPredicates.Any(predicate => predicate(item, searchWord))))).OrderBy(p => p, _entryComparer).ToList();
							else
								result = (isSearchEmpty ? allItems.Where(p => tuplePredicate(p)).OrderBy(p => p, _entryComparer) : allItems.Where(p => tuplePredicate(p)).Where(item => search.All(searchWord => generalPredicates.Any(predicate => predicate(item, searchWord))))).OrderBy(p => p, _entryComparer).ToList();
						}
					}

					if (currentSearch != _searchItemsFilter) {
						WpfUtilities.TextBoxOk(_tbSearchItems);
						return;
					}

					_items.Dispatch(r => r.ItemsSource = new RangeObservableCollection<TValue>(result));
					WpfUtilities.TextBoxOk(_tbSearchItems);
					OnFilterFinished(result);
				}
				catch {
					WpfUtilities.TextBoxOk(_tbSearchItems);
				}
				finally {
					IsFiltering = false;
				}
			}
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
				 let val = (string) attributeCopy.AttachedAttribute 
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

		private List<string> _getSearch(string currentSearch) {
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
