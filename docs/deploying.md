# Deploying Classroom Toolkit in a school

Classroom Toolkit is a single `ClassroomToolkit.exe` with no installer. It
needs no administrator rights, writes only to each user's
`Documents\Classroom Toolkit` folder (or a portable `Data` folder) and never
uses the network. Deploying it is a file copy plus, if you like, a startup
entry.

This guide uses Active Directory Group Policy Preferences to start the toolkit
in the notification area (without opening the launcher) when a member of a
chosen security group signs in.

## Command-line options

| Command | What it does |
|---|---|
| `ClassroomToolkit.exe` | Starts in the notification area. Opens the launcher only if the user has ticked **Show window when starting**. |
| `ClassroomToolkit.exe --tray` | Starts in the notification area and never opens the launcher at start-up. |
| `ClassroomToolkit.exe --show` | Starts and opens the launcher. |
| `ClassroomToolkit.exe --selftest <folder>` | Opens every tool once and saves a screenshot of each, with a log. Useful for checking a new build. |

Only one copy runs per user: starting it again brings up the copy that's
already running.

## 1. Put the program on each PC

Running the program straight from a network share works too (see
[below](#alternative-run-it-from-a-network-share)), but copying it to each
computer is more reliable: it starts faster, there are no security prompts
about files from the network, and it keeps working if the share is slow at
logon.

1. Put `ClassroomToolkit.exe` on a share that computers can read, for example
   `\\yourserver\Software$\ClassroomToolkit\ClassroomToolkit.exe`. Give the
   computer accounts Read access: **Domain Computers** is enough.
2. In **Group Policy Management**, create or edit a GPO linked to the OU that
   contains the classroom **computers**.
3. Go to **Computer Configuration → Preferences → Windows Settings → Files**
   and choose **New → File**:
   - **Action:** Update
   - **Source file:** `\\yourserver\Software$\ClassroomToolkit\ClassroomToolkit.exe`
   - **Destination file:** `C:\Program Files\Classroom Toolkit\ClassroomToolkit.exe`

To update, replace the file on the share. PCs pick it up at their next policy
refresh or restart.

## 2. Start it at logon for a group

1. Edit a GPO linked to the OU that contains the **users** (this can be a
   different GPO from step 1).
2. Go to **User Configuration → Preferences → Windows Settings → Registry**
   and choose **New → Registry Item**:
   - **Action:** Replace
   - **Hive:** HKEY_CURRENT_USER
   - **Key path:** `Software\Microsoft\Windows\CurrentVersion\Run`
   - **Value name:** `ClassroomToolkit`
   - **Value type:** REG_SZ
   - **Value data:** `"C:\Program Files\Classroom Toolkit\ClassroomToolkit.exe" --tray`
3. On the **Common** tab:
   - tick **Item-level targeting**, click **Targeting…**, then
     **New Item → Security Group**, and pick your group;
   - tick **Remove this item when it is no longer applied**, so the entry is
     removed again if someone leaves the group.

Use the value name `ClassroomToolkit` exactly. It's the name the program's
own **Start with Windows** option uses, so that option shows as ticked and
the program never starts twice.

## Things to know

- **If a teacher unticks Start with Windows**, the policy puts it back at the
  next refresh. To let staff opt out, set the Action to **Create** instead:
  the entry is then only added when it isn't already there.
- **Settings and lists** are saved per user in `Documents\Classroom Toolkit`,
  which works with folder redirection, so staff keep their lists wherever
  they sign in. Don't create a `Data` folder next to the exe: that switches
  the program to portable mode, with one shared settings folder.
- **Unsigned file:** the exe isn't code-signed. Before putting it on the
  share, right-click it, choose **Properties** and tick **Unblock** (if it's
  there), so the "downloaded from the internet" mark isn't copied to every PC
  with it.

## Alternative: run it from a network share

Skip step 1 and, in step 2, point **Value data** at the share:

```
"\\yourserver\Software$\ClassroomToolkit\ClassroomToolkit.exe" --tray
```

This suits fixed classroom PCs on the school network. For laptops that leave
site, copy the program to each PC instead.

- **Permissions:** the staff group needs **Read** access to the share and
  folder, because each user runs the file themselves.
- **Security warning:** Windows may show an *Open File – Security Warning* at
  every logon if it thinks the server is outside your network (this usually
  happens with a full name such as `server.school.local`). To stop it, go to
  **User Configuration → Administrative Templates → Windows Components →
  Internet Explorer → Internet Control Panel → Security Page → Site to Zone
  Assignment List** and add `file://yourserver` with the value `1` (or the
  full name, if that's what the path uses).
- **Updating:** Windows won't let you overwrite the exe while anyone has it
  open. Replace it out of hours, or keep each version in its own folder
  (for example `...\ClassroomToolkit\1.1.5\`) and change the path in the GPO,
  which also makes it easy to go back to an earlier version.
- **Settings:** each user's lists still go to their own Documents folder. Don't
  put a `Data` folder beside the exe on the share, or everyone would share one
  settings folder.
- **When the share can't be reached:** a laptop away from school, or a PC that
  signs in before the network is ready, won't start the toolkit, and no error
  is shown. If it sometimes fails to appear after a fast logon, enable
  **Computer Configuration → Administrative Templates → System → Logon →
  Always wait for the network at computer startup and logon**.

## Testing

1. Add yourself to the group.
2. On a PC, run `gpupdate /force`, then sign out and back in. The traffic-light
   icon should appear by the clock.
3. If it doesn't, check that the exe exists where the registry entry points,
   and run `gpresult /r` to confirm both GPOs applied.
