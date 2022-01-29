###################################################################################################
# This script generates Teams_2021.txt from Teams_2021_Raw.txt by removing all unknown team codes.
#
# Both of these text files are useful in different situations:
#     * Teams_2021_Raw.txt
#         - All unknown team codes are assigned a name string equal to the code itself (in hex).
#         - This can be useful during development (when we are trying to pair each team name with
#           a code). However, it causes team dropdown menus to become unresponsive.
#     * Teams_2021.txt
#         - Only the known team codes are assigned names.
#         - This makes the team dropdown menus responsive, but many teams may be missing.
###################################################################################################

import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
IN_FILENAME = os.path.join(SCRIPT_DIR, "Teams_2021_Raw.txt")
OUT_FILENAME = os.path.join(SCRIPT_DIR, "Teams_2021.txt")

with open(IN_FILENAME) as in_file:
    with open(OUT_FILENAME, 'w') as out_file:
        lines = in_file.readlines()
        
        # Try interpreting the first 4 digits as hex
        for line in lines:
            try:
                int(line[:4], 16)
            except ValueError:
                # If it's not 4-digit hex, then it must be a team name => Write to file.
                out_file.write(line)
                
                # Sanity check: No team name contains a number
                team_name = line.split(',')[0]
                assert not any(char.isdigit() for char in team_name[:-2]), f"Number found in team name: {team_name}"
            else:
                # Sanity check: All the hex code letters are uppercase
                assert line[:4] == line[:4].upper(), f"Lowercase letter found in hex code: {line[:4]}"