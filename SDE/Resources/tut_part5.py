"""

								Tutorial - Part 5
								Loading tables
								
"""

# You can import fields or merge tables using the following commands.
# The "r" in front of the string is to escape the string, without it the path
# would have to be LoadTable("C:\\path\\to\\num2cardillustnametable.txt")

custom_table = database.LoadTable(r"C:\path\to\num2cardillustnametable.txt")

for tuple in custom_table:
	if (item_db.ContainsKey(tuple.Key)):
		client_items[tuple.Key, "illustration"] = tuple["illustration"]
