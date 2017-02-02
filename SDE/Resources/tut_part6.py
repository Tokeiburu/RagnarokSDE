"""

								Tutorial - Part 6
								Composite tables
								
"""

# The following tables can be merged together :
# item_db + item_db2 = item_db_m
# mob_skill_db + mob_skill_db2 = mob_skill_db_m
# mob_db + mob_db2 = mob_db_m

# A composite table can only be read from, you cannot set values to it.
# They will be automatically updated if you modify their original tables (item_db
# or item_db2).

item_db[499, "name"] = "Item from item_db"
item_db2[499, "name"] = "Item from item_db2 !"
print item_db_m[499, "name"]
item_db2[499] = None
print item_db_m[499, "name"]

