# Desktop Notes WPF

Utility that draws text on a window that is sent to back. Useful for placing persistent notes. Alternative to https://github.com/tofuman0/Desktop-Notes which can have issues working in RDS environments.

## Referencing external files

You are able to reference external files by entering \{\{ref=PATH_TO_FILE\}\} into the desktop note text. For example:
> \{\{ref=\\\\server\\folder\\file.txt\}\}

> \{\{ref=c:\\folder\\file.txt\}\}

> \{\{ref=http://url.tld/feed.php \}\}

## Referencing system details

You are able to reference system details such as computer name, date, time, CPU, Ram and HDDs by entering the below into the desktop text:

\{\{ref=datetime\}\}
\{\{ref=datetime("dd-MM-yyyy HH:mm:ss")\}\}
\{\{ref=system("computername")\}\}
\{\{ref=system("ram")\}\}
\{\{ref=system("cpu")\}\}
\{\{ref=system("hdd")\}\}