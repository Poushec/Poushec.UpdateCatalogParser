# UpdateCatalogLib

This library was made to help me or anyone else to collect data from https://www.catalog.update.microsoft.com/ in a comfortable way.

## Credits

Big thanks to ryan-jan (https://github.com/ryan-jan) for his MSCatalog PowerShell module. My original plan was to re-write this module as C# class library.)

## Other implementations

- **Java**: [zaladhaval/UpdateCatalogParseLib](https://github.com/zaladhaval/UpdateCatalogParseLib)

## How to use it

``` C#
CatalogClient catalogClient = new CatalogClient(new HttpClient(), pageReloadAttemptsAllowed = 3);
List<CatalogSearchResult> searchResults = await catalogClient.SendSearchQueryAsync("SQL Server 2019", ignoreDuplicates = true);
```
This method will return you collection of CatalogSearchResult objects. Each of this objects represent search result from catalog.update.microsoft.com with data available through
search results page only: 

1. Update's ID
2. Title
3. Products
4. Classification
5. Last Updated date
6. Version
7. Size
8. Size in bytes

Use the `pageReloadAttemptsAllowed` optional parameter to set the number of page refresh attempts allowed. Catalog can be unreliable at times and you almost certain to get at least one invalid page from it when working with a lot of search results. Default value: 3 

If `ignoreDuplicates` optional parameter is TRUE than function will return only updates with unique `Title` and `SizeInBytes` fields. Default: TRUE

To get more info on a particular update (which you would normally get by following the link on the results list) pass one of the CatalogSearchResult objects you've got from SendSearchQueryAsync to this method: 

``` C#
UpdateBase updateDetails = await catalogClient.GetUpdateDetailsAsync(CatalogSearchResult searchResult)
```

It will get you an object derived from `UpdateBase` class (ether `Update` or `Driver`) with all information available about it from details and download pages, for example download links, HardwareIDs if it is a driver, Supersedes list if it is an Update etc. 

## Example Usage

``` C#
using Poushec.UpdateCatalog;
using Poushec.UpdateCatalog.Models;

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
