# Desktop Notes WPF

Utility that draws text on a window that is sent to back. Useful for placing persistent notes. Alternative to https://github.com/tofuman0/Desktop-Notes which can have issues working in RDS environments.

## Referencing external files

You are able to reference external files by entering \{\{ref=PATH_TO_FILE\}\} into the desktop note text. For example:
> \{\{ref=\\\\server\\folder\\file.txt\}\}

> \{\{ref=c:\\folder\\file.txt\}\}

> \{\{ref=http://url.tld/feed.php \}\}

