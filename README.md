# AI_EnvironmentalSeasons

# Introduction
This plugin fixes several issues with AI Shoujo's environmental simulator.  Illusion uses a pretty sophisticated environmental simulator for AI Shoujo, but they didn't set it up correctly (or at all).  They set the world coordinates to 0 degrees longitude, 0 degrees latitude.  They set the starting day to January 1st, 1AD... and in Groundhog Day fashion, at the start of each new day it gets set back to January 1st. As a result, the environment simulates a perpetual winter on the equator. This plugin sets the world coordinates to an island off the coast of Okinawa.  It sets the starting day to early spring of the current year, and correctly increments days.  The temperature profile has been edited to change with the seasons.  Summers will be hot, Winters will be cold.

# Changes
Latitude and Longitude set to an island off the coast of Okinawa (configurable)<br>
New games will begin in early spring of the current year (configurable), games created before this plugin will also set their date to the same.<br>
Days will now correctly increment.  The sun will no longer rise in the exact same spot every day.  Days will get longer in the summer and shorter in the winter.<br>
Temperature where girls get cold reduced to 18 degrees C (configurable).  Default was 23 degrees C which didn't seem right.<br>
Temperature where girls get hot reduced to 27 degrees C (configurable).  Default was 30 degrees C which didn't seem right.<br>
Cellphone UI will display the current day.<br>
A new temperature profile has been created that varies with time and more closely matches the area.  Generally speaking temperatures will be cold in the winter, hot in the summer and vary between.<br>
