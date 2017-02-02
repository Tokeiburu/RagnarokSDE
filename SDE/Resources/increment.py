"""
 
                     ### Sample script ###
 
 
 This sample script is based on IronPython, an open-source 
 implementation of the Python programming language focussing 
 on the .NET Framework.
 
 Uncomment the lines at the end of this file to execute one
 of the samples below.
 
 
                     ### Documentation ###

 selected_db : The currently selected database table.
 script : The script helper object, used to terminate 
          the script, throw exceptions, show dialogs or 
          request user inputs.
 selection : A list containing all the currently selected tuples;
             the tuples are ordered by their IDs.
 database : The project database component (currently not useful).
 
 All the databases can be accessed via their filename. For example :
 item_db.txt would be item_db. They have the following accessors:
	table[id, property] = value
	table[id] = tuple
	
 For the tuples, the accessors go as :
	tuple[property] = value
	
 Most of the values are strings, but some are ints (such as the id).
 Many of them are interchangeable, meaning you can put either an int
 or a string and the value will be converted accordingly.
 
 You can access composite tables (ex: mob_db + mob_db2) with 
 mob_db_m. However these tables are readonly.
 
"""

def sample_update_properties():
	if (selected_db != item_db):
		script.throw("Please select the item_db table")
		
	if (selection.Count == 0):
		script.throw("Please select the elements you want to change")
		
	id = 0
	
	for tuple in selection:
		# You can access the tuples by their IDs; these correspond,
		# most of the time, to those found in your db file, for 
		# example : "503,Yellow_Potion,Yellow Potion,0,550,..."
		# 503 = ["item_id"] = [0]
		# Yellow_Potion = ["aegis_name"] = [1]
		# etc
		tuple["aegis_name"] = "test_item_" + str(id)
		tuple["name"] = client_items[tuple.Key, "display_name"].replace(" ", "_")
		tuple["equip_level"] = 75
		
		tuple["type"] = 5
		id = id + 1
	return
	
def sample_selection():
	# You can manipulate the currently selected elements with "selection"
	# The following will select the next 5 entries in the table
	lastKey = selection[selection.Count - 1].Key
	
	selection.Clear()
	
	for key in range(1, 5):
		if (item_db.ContainsKey(lastKey + key)):
			selection.Add(item_db[lastKey + key])
	return
	
def sample_copy_to():
	# You can easily copy tuples from one table to another.
	item_db2[510] = item_db[501]
	
	# Remove the last letter
	item_db2[510, "name"] = item_db2[510, "name"][:-1]
	return

def sample_uppercase():
	# This script will uppercase all your item names
	
	for tuple in item_db2:
		tuple["name"] = tuple["name"].upper()
		
	return
	
def sample_user_input():
	# This sample demonstrates the usage of the script object.
	value = script.input("Window title", "Please enter a value", "default")
	
	script.show("The value entered is '{0}'", value);
	
	if (script.confirm("Are you sure you want to terminate?") == True):
		script.throw("The script has been terminated.")
		
	script.exit()
	return
	
def sample_using_commands():
	# WARNING : it's best to NOT use the following commands.
	# All of the commands below can be achieved by using the
	# object accessors (ex: tuple["attribute"] = value).
	# 
	# The command object is what SDE uses to apply operations
	# on the tables. For completeness, they're shown below but
	# they're not recommended.
	
	selected_db.Commands.Delete(501)
	selected_db.Commands.CopyTupleTo(502, 501)
	selected_db.Commands.Set(selected_db[501], 2, "Purple Potion")
	selected_db.Commands.ChangeKey(501, 500)
	item_db2.Commands.CopyTupleTo(selected_db, 500, 499)
	
	# The suggested approach/equivalent is the following :
	#selected_db.Remove(501)
	#selected_db[501] = selected_db[502]
	#selected_db[501, "name"] = "Purple Potion"
	#selected_db[501, "item_id"] = 500
	#item_db2[499] = selected_db[500]
	
	return

# Main body of the script. You do not need to define methods
# for your scripts. This simply makes the tutorials easier.
# 
# Uncomment the samples you want to test.

sample_update_properties()
#sample_selection()
#sample_copy_to()
#sample_user_input()
#sample_using_commands()


