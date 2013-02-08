# GitDoc

GitDoc is a small Windows console app that you can execute 
from a directory and it will take any Markdown file it finds (maintaining the same directory structure) 
and output the parsed results via GitHub's own Markdown API to a folder of your choice.

The result? Awesome documentation.

It searches for Markdown files from the current working directory:

	C:\Dev\MyProject\docs> tools\gitdoc

In this case, it'll execute `.\tools\gitdoc.exe` but GitDoc will discover
files in the `docs` folder.

You can also pass in the directory you want to process:

	gitdoc /b "C:\MyProject"

And the output directory:

	gitdoc /o "C:\docs"

Or both:

	gitdoc /b ".\docs" /o ".\docs\output"

The paths can be relative or absolute.

# Features

- Preserves directory structure of the Markdown files it discovers
- Provides some convenient `{tokens}` you can find and replace
	- `{author}` - The current process' identity
	- `{date}` - The current `DateTime.Now` timestamp
- Processes relative links by looking for links to other `.md` files
- OAuth increased rate limit support

**Note:** GitDoc only supports `.md` files at the moment since that's
what I use. It would be trivial to add other extensions.

# OAuth

The default rate limit for GitHub is 60 requests per hour. Chances are, you're going to
go above that.

If you pass in your OAuth client secret and ID to GitDoc, it will use it for you
and thereby increase your rate limit to 5000 requests per hour.

To create a client ID and secret, you'll have to do so in your [GH account settings](https://github.com/settings/applications/new).

Pass them in like this:

	gitdoc /clientid xxx /clientsecret xxx

*Note:* It's not good to distribute these publicly, but within a team it's probably fine. It's up to you.

# Sublime Text 2

I primarily use this to easily create and process a document repository
for storage in source control.

Here's what my Sublime Text 2 project file looks like:

	{
		"build_systems":
		[
			{
				"name": "Markdown",
				"cmd": ["tools\\gitdoc", "-ClientId", "xxx", "-ClientSecret", "xxx"]
			}
		]
	}

Then select your build system in `Tools > Build System > Markdown`. Press Ctrl-B to build.