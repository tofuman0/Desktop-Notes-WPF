# Desktop Notes WPF

Utility that draws text on a window that is sent to back. Useful for placing persistent notes. Alternative to https://github.com/tofuman0/Desktop-Notes which can have issues working in RDS environments.

## Referencing external files

You are able to reference external files by entering \{\{ref=PATH_TO_FILE\}\} into the desktop note text. For example:
> \{\{ref=\\\\server\\folder\\file.txt\}\}

> \{\{ref=c:\\folder\\file.txt\}\}

> \{\{ref=http://url.tld/feed.php \}\}

## Referencing system details

You are able to reference system details such as computer name, date, time, CPU, Ram and HDDs by entering the below into the desktop text:

> \{\{ref=datetime\}\}

> \{\{ref=datetime("dd-MM-yyyy HH:mm:ss")\}\}

> \{\{ref=system("computername")\}\}

> \{\{ref=system("ram")\}\}

> \{\{ref=system("cpu")\}\}

> \{\{ref=system("hdd")\}\}

> \{\{ref=system("hdd","c","d")\}\}

## Referencing JSON data

You are able to reference JSON data obtained via HTTP(S). Nested values are accessible by adding the property names after the URL. If no properties are provided the RAW JSON data is displayed

> \{\{ref=json("http://weatherdomain.tld/jsondata.json") \}\}

> \{\{ref=json("http://weatherdomain.tld/jsondata.json","current_weather","temperature") \}\}

You can set prefix and suffix for array values that are returned and set whether to newline on elements using elementsuffix= and elementprefix= before the properties and/or newlineseparator=true newlineseparator=false to set whether or not a new line is placed after each element.

For example if the data provider allows you to pull a number of days min or max temperature values you may want to add a celcious suffix

> Min Temp: \{\{ref=json("http://weatherdomain.tld/jsondata.json&daily=temperature_2m_min&timezone=auto&forecast_days=2","elementsuffix=°c","newlineseparator=true","daily","temperature_2m_min") \}\}
