# Update Catalog Parser

This library was made to help me or anyone else to collect data from https://www.catalog.update.microsoft.com/ in a comfortable way.

## Credits

Big thanks to ryan-jan (https://github.com/ryan-jan) for his MSCatalog PowerShell module. My original plan was to re-write this module as C# class library.)

## Links 

<img src="https://upload.wikimedia.org/wikipedia/commons/2/25/NuGet_project_logo.svg" alt= “” width="16" height="16"> [This package on NuGet](https://www.nuget.org/packages/Poushec.UpdateCatalogParser/)

## Other implementations

- **Java**: [zaladhaval/UpdateCatalogParseLib](https://github.com/zaladhaval/UpdateCatalogParseLib)

## How to use it

Instantiate the CatalogClient first:

``` C#
CatalogClient catalogClient = new CatalogClient(new HttpClient(), pageReloadAttemptsAllowed = 3);
```
(Optional) If you want CatalogClient to use a particular instance of HttpClient you can pass it as a first argument to the constructor. If you will not provide it - it will be created for you.

(Optional) Use the `pageReloadAttemptsAllowed` argument to set the number of page refresh attempts allowed. Catalog can be unreliable at times and you almost certain to get at least one invalid page from it when working with a lot of search results. Default value: 3 

``` C#
List<CatalogSearchResult> searchResults = await catalogClient.SendSearchQueryAsync("SQL Server 2019", ignoreDuplicates = true);
```
This method will load and parse all available search result pages and return you a collection of CatalogSearchResult objects. Each of this objects represent search result from catalog.update.microsoft.com with data available through
search results page only: 

1. Update's ID
2. Title
3. Products
4. Classification
5. Last Updated date
6. Version
7. Size
8. Size in bytes

(Optional) If `ignoreDuplicates` argument is TRUE than function will return only updates with unique `Title` and `SizeInBytes` fields. Default: TRUE

If you're only interested in results from a first page (or just how many results there are) use this method instead: 

``` C#
CatalogResponse firstResultsPage = await catalogClient.GetFirstPageFromSearchQueryAsync("SQL Server 2019");
```

This method returns a CatalogResponse object representing the current search results page, specifically: 

1. Collection of CatalogSearchResult objects which holds search results from current page (`SearchResults`)
2. Total search query results count (`ResultsCount`)
3. The field indicating if it is a final page (`FinalPage`)

To get more info on a particular update (which you would normally get by following the link on the results list) pass one of the CatalogSearchResult objects you've got from SendSearchQueryAsync to this method: 

``` C#
UpdateBase updateDetails = await catalogClient.GetUpdateDetailsAsync(CatalogSearchResult searchResult)
```

It will get you an object derived from `UpdateBase` class (ether `Update` or `Driver`) with all information available about it from details and download pages, for example download links, HardwareIDs if it is a driver, Supersedes list if it is an Update etc. 

## Example Usage

``` C#
using Poushec.UpdateCatalogParser;
using Poushec.UpdateCatalogParser.Models;

var rand = new Random();
var client = new HttpClient();

var catalogClient = new CatalogClient(client);

var testSearchResults = await catalogClient.SendSearchQueryAsync("August 2021 Drivers", false);
            
Console.WriteLine($"{testSearchResults.Count} updates founded. Random update:\n");

var randomSearchResult = testSearchResults[rand.Next(0, testQuery.Count)];
var testDetailedUpdate = await catalogClient.GetUpdateDetailsAsync(client, randomSearchResult) as Driver; //We're probably won't find anything but drivers by this query)

Console.WriteLine($"{testDetailedUpdate.Title}\n\n{String.Join("\n", testDetailedUpdate.HardwareIDs)}");
```

Output: 

```
51 updates founded. Random update:

Kyocera Mita Corporation - Printers - Kyocera EP 510DN KX

USBPRINT\KYOCERAEP_510DNB229
LPTENUM\KYOCERAEP_510DNB229
```

## Limitations

What this library does - is parse HTML pages from catalog.update.microsoft.com, so it derives it's limitations directly from it. For example, it cannot return more than 1000 search results from a single query.
