"""

								Tutorial - Part 4
								Using dialogs
								
"""

value = script.input("Window title", "Please enter a value", "default")

script.show("The value entered is '{0}'", value);

if (script.confirm("Are you sure you want to terminate?") == True):
	script.throw("The script has been terminated.")
	
script.exit()


