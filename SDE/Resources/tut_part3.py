"""

								Tutorial - Part 3
								Creating new entries
								
"""

# Copying entries is very simple; all you have to do is assign
# the ID from another tuple.

item_db[500] = item_db[501]
item_db[500, "name"] = item_db[500, "name"].replace("Red", "Purple")

# To add a new empty item :
item_db.Add(499)

# To delete an item :
item_db.Delete(500)
item_db[500] = None

# You can, of course, copy items from one table to another

for tuple in item_db2:
	item_db[tuple.Key] = tuple

# Copy items from item_db to item_db2

for tuple in selection:
	item_db2[tuple.Key] = tuple

