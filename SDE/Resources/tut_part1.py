"""

								Tutorial - Part 1
								Changing tuple values
								
"""

# Here are some definitions used throughout the guide :
# - A database (default.sde) contains tables.
# - A table (item_db, skill_db, etc) contains rows, called tuples.
# - A table contains multiple columns, called attributes.
# - An attribute can take multiple values, ex: item_id, name, number_of_slots, etc

# All the tables can be accessed via their filename. For example :
# item_db.txt/item_db.conf would be item_db. Other tables would be
# mob_db, skill_db, mob_skill_db, etc.

item_db[501, "name"] = "Test Item!"
skill_db[7, "element"] = "8"  # 8 = Ghost

# The accessors go as follow :
# table_db[id, "attribute"] = value
# table_d[id] = tuple
# tuple["attribute"] = value
# Example :

item_db[501, "Number of slots"] = 5
item_db[501, "number_of_slots"] = 5
item_db[501, "10"] = 5
item_db[501, 10] = 5

tuple = item_db[501]
tuple["aegis_name"] = "test_1"
tuple["aegis_name"] = tuple["aegis_name"].upper()

