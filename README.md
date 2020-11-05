# memulator
Motorola Emulator (6800/6809 - FLEX/OS9/UniFLEX))

Instructions for running memulator.exe under Mono-Develop debugger on linux (Ubuntu)

Step 1.
    Install mono-complete and monodevelop

        sudo apt install mono-complete
        sudo apt install monodevelop

Step 2.
    Install xterm

        sudo apt-get install xterm

Step 3.
    Since version 20.04 gnome-terminal-server had moved from /usr/lib/gnome-terminal/ to /usr/libexec/
    so we need to get a copy of it where Mono-Develop is expecting to find it.

        cd /usr/lib
        sudo mkdir gnome-terminal
        sudo cp /usr/libexec/gnome-terminal-server /usr/lib/gnome-terminal/gnome-terminal-server

Step 4.
    Extract the memulator-linux source files to your user's development area and use Mono-Develop to load the
    solution file. Run it in the debugger once to get: (it will fail - abort run)
        /home/<username>/.config/EvensonConsultingServices/SWTPCmemulator/ directory created and
        defaultConfiguration.txt file copied to your development work area

Step 5.
    copy defaultConfiguration.txt to /home/<username>/.config/EvensonConsultingServices/SWTPCmemulator/defaultConfiguration.txt

    copy CONFIGFILES, DISKS, and ROMS to either /usr/share/EvensonConsultingServices/SWTPCmemulator/
                                      or        /home/mevenson/EvensonConsultingServices/SWTPCmemulator/

You should now be able to run the emulator in the debugger.

Explanation:
    The application will search first for /usr/share/EvensonConsultingServices/SWTPCmemulator/ and if not found will
    search for /home/username>/EvensonConsultingServices/SWTPCmemulator/. If neither of these exist, it will use the 
    current execution directory of the application as the location for thsese directories. Use the first location if
    you want every user to share the directories - bad idea. Use the second to isolate the use of these directories
    to the logged in user - best idea.

Mike
