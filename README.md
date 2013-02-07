# GitDoc

GitDoc is a small Windows console app that you can execute 
from a directory and it will output any Markdown file it finds 
and output the parsed results via GitHub's own Markdown API.

It searches for Markdown files from the current working directory:

	C:\Dev\MyProject\docs> tools\gitdoc

In this case, it'll execute `.\tools\gitdoc.exe` but GitDoc will discover
files in the `docs` folder.

You can also pass in the directory you want to process:

	gitdoc -b "C:\MyProject"

And the output directory:

	gitdoc -o "C:\docs"

Or both:

	gitdoc -b ".\docs" -o ".\docs\output"

The paths can be relative or absolute.

# Features

- Preserves directory structure of the Markdown files it discovers
- Provides some convenient `{tokens}` you can find and replace
	- `{author}` - The current process' identity
	- `{date}` - The current `DateTime.Now` timestamp
- Processes relative links by looking for links to other `.md` files

**Note:** GitDoc only supports `.md` files at the moment since that's
what I use. It would be trivial to add other extensions.

# Sublime Text 2

I primarily use this to easily create and process a document repository
for storage in source control.

Here's what my Sublime Text 2 project file looks like:

	{
		"build_systems":
		[
			{
				"name": "Markdown",
				"cmd": ["tools\\gitdoc"]
			}
		]
	}

Then select your build system in `Tools > Build System > Markdown`. Press Ctrl-B to build.