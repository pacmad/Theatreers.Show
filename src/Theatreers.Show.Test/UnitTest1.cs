using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Theatreers.Core.Abstractions;
using Theatreers.Show.Abstractions;
using Theatreers.Show.Models;
using Theatreers.Core.Providers;
using Xunit;
using Theatreers.Show.Utils;
using Microsoft.Azure.Documents;
using Theatreers.Core.Models;
using Theatreers.Show.Actions;
using Dynamitey.DynamicObjects;
using System.Collections.Generic;
using System.Collections;

namespace Theatreers.Show.Test
{
  public class UnitTest1 : IAsyncLifetime
  {
    private static string _databaseId = "theatreers";
    private static string _imageCollectionName = "shows";
    private static string _newsCollectionName = "shows";
    private static string _showCollectionName = "shows";
    private static string _showlistCollectionName = "showlist";
    public static Uri _imageCollectionUri = UriFactory.CreateDocumentCollectionUri(_databaseId, _imageCollectionName);
    private static Uri _newsCollectionUri = UriFactory.CreateDocumentCollectionUri(_databaseId, _newsCollectionName);
    private static Uri _showCollectionUri = UriFactory.CreateDocumentCollectionUri(_databaseId, _showCollectionName);
    private static Uri _showlistCollectionUri = UriFactory.CreateDocumentCollectionUri(_databaseId, _showlistCollectionName);
    private static IShowDomain _showDomain;
    private static List<string> ids = new List<string>() { "1", "2", "3", "4" };


    public async Task InitializeAsync()
    {
      IDocumentClient client = new DocumentClient(new System.Uri("https://localhost:8081"), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
      IStorageProvider<ImageObject> _imageStore = new CosmosStorageProvider<ImageObject>(client, _imageCollectionUri, _databaseId, _imageCollectionName);
      IStorageProvider<NewsObject> _newsStore = new CosmosStorageProvider<NewsObject>(client, _newsCollectionUri, _databaseId, _newsCollectionName);
      IStorageProvider<ShowObject> _showStore = new CosmosStorageProvider<ShowObject>(client, _showCollectionUri, _databaseId, _showCollectionName);
      IStorageProvider<ShowListObject> _showListStore = new CosmosStorageProvider<ShowListObject>(client, _showlistCollectionUri, _databaseId, _showlistCollectionName);
      IDataLayer _dataLayer = new DataLayer(_imageStore, _newsStore, _showStore, _showListStore);
      _showDomain = new ShowDomain(_dataLayer);

      ILogger log = new StubLogger();

      string databaseName = "theatreers";
      string collectionName = "shows";

      IDocumentClient documentClient = new DocumentClient(new Uri("https://localhost:8081"), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
      Uri showCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseName, collectionName);
      Uri databaseUri = UriFactory.CreateDatabaseUri(databaseName);

      Database theatreersDatabase = new Database()
      {
        Id = databaseName
      };

      await documentClient.CreateDatabaseIfNotExistsAsync(theatreersDatabase);

      DocumentCollection showCollection = new DocumentCollection()
      {
        Id = collectionName
      };

      showCollection.PartitionKey.Paths.Add("/partition");
      showCollection.DefaultTimeToLive = -1;
      await documentClient.CreateDocumentCollectionIfNotExistsAsync(databaseUri, showCollection);


      foreach (string id in ids){
        await _showDomain.CreateShowObject(new MessageObject<ShowObject>()
        {
          Body = new ShowObject()
          {
            Id = id,
            Partition = id,
            Doctype = DocTypes.Show
          },
          Headers = new MessageHeaders()
          {
            RequestCorrelationId = Guid.NewGuid().ToString(),
            RequestCreatedAt = DateTime.Now
          }
        });
        int childRecords = 0;
        Int32.TryParse(id, out childRecords);
        childRecords *= childRecords;

        for (int i = 0; i < childRecords; i++)
        {
          await _showDomain.CreateImageObject(new MessageObject<ImageObject>()
          {
            Body = new ImageObject()
            {
              Id = Guid.NewGuid().ToString(),
              Partition = id,
              Doctype = DocTypes.Image,
              ContentUrl = "https://localhost/image.jpg",
              Name = "Example Image"
            },
            Headers = new MessageHeaders()
            {
              RequestCorrelationId = Guid.NewGuid().ToString(),
              RequestCreatedAt = DateTime.Now
            }
          }, log);


          await _showDomain.CreateNewsObject(new MessageObject<NewsObject>()
          {
            Body = new NewsObject()
            {
              Id = Guid.NewGuid().ToString(),
              Partition = id,
              Doctype = DocTypes.News,
              DatePublished = DateTime.Now.ToString(),
              Url = "http://somenewssite.com",
              Name = "Some News Article"
            },
            Headers = new MessageHeaders()
            {
              RequestCorrelationId = Guid.NewGuid().ToString(),
              RequestCreatedAt = DateTime.Now
            }
          }, log);
        }
      }
    }

    public async Task DisposeAsync()
    {
      string databaseName = "theatreers";
      string collectionName = "shows";

      IDocumentClient documentClient = new DocumentClient(new Uri("https://localhost:8081"), "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
      Uri showCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseName, collectionName);
      Uri databaseUri = UriFactory.CreateDatabaseUri(databaseName);

      await documentClient.DeleteDatabaseAsync(databaseUri);
    }


    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    public async Task CheckImagesExistByShowIdThatExists(string showId)
    {
      // Arrange
      int idAsInteger = 0;
      int expectedCount = 0;
      Int32.TryParse(showId, out idAsInteger);
      expectedCount = idAsInteger * idAsInteger;

      // Act  
      ICollection<ImageObject> imageObjects = await _showDomain.GetImageByShow(showId);

      //Assert
      Assert.Equal(expectedCount, imageObjects.Count);
    }


    [Theory]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("8")]
    public async Task CheckImagesExistByShowIdThatDoesntExist(string showId)
    {
      // Arrange
      int idAsInteger = 0;
      Int32.TryParse(showId, out idAsInteger);

      // Act  
      ICollection<ImageObject> imageObjects = await _showDomain.GetImageByShow(showId);

      //Assert
      Assert.Equal(0, imageObjects.Count);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    public async Task CheckNewsExistsByShow(string showId)
    {
      // Arrange
      int idAsInteger = 0;
      int expectedCount = 0;
      Int32.TryParse(showId, out idAsInteger);
      expectedCount = idAsInteger * idAsInteger;

      // Act  
      ICollection<NewsObject> newsObjects = await _showDomain.GetNewsByShow(showId);

      //Assert
      Assert.Equal(expectedCount, newsObjects .Count);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    public async Task CheckShowExists(string showId)
    {
      // Arrange
      int idAsInteger = 0;
      Int32.TryParse(showId, out idAsInteger);

      // Act  
      ShowObject showObject = await _showDomain.GetShow(showId);

      //Assert
      Assert.NotNull(showObject);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("8")]
    public async Task CheckShowThatDoesntExistReturnsNull(string showId)
    {
      // Arrange
      int idAsInteger = 0;
      Int32.TryParse(showId, out idAsInteger);

      // Act  
      ShowObject showObject = await _showDomain.GetShow(showId);

      //Assert
      Assert.Null(showObject);
    }

    [Fact]
    public async Task CanCreateImageWithValidData()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      ImageObject imageObject = new ImageObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Doctype = DocTypes.Image,
        Name = "Example Image",
        ContentUrl = "http://localhost/something.jpg"
      };

      MessageObject<ImageObject> message = new MessageObject<ImageObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = imageObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateImageObject(message, log));

      // Assert
      Assert.Null(ex);
      Assert.Equal(1, (await _showDomain.GetImageByShow(showId)).Count);
    }

    [Fact]
    public async Task CanCreateNewsObjectWithValidData()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      NewsObject newsObject = new NewsObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Doctype = DocTypes.News,
        Name = "Example Image",
        Url = "http://somenewswebsite.co.uk/news",
        DatePublished = DateTime.Now.ToString()
      };

      MessageObject<NewsObject> message = new MessageObject<NewsObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = newsObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateNewsObject(message, log));

      // Assert
      Assert.Null(ex);
      Assert.Equal(1, (await _showDomain.GetNewsByShow(showId)).Count);
    }

    [Fact]
    public async Task CannotCreateNewsObjectWithoutId()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      NewsObject newsObject = new NewsObject()
      {
        Partition = showId,
        Doctype = DocTypes.News,
        Name = "Example Image",
        Url = "http://somenewswebsite.co.uk/news",
        DatePublished = DateTime.Now.ToString()
      };

      MessageObject<NewsObject> message = new MessageObject<NewsObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = newsObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateNewsObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetNewsByShow(showId)).Count);
    }

    [Fact]
    public async Task CannotCreateNewsObjectWithoutPartition()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      NewsObject newsObject = new NewsObject()
      {
        Id = Guid.NewGuid().ToString(),
        Doctype = DocTypes.Image,
        Name = "Example Image",
        Url = "http://somenewswebsite.co.uk/news",
        DatePublished = DateTime.Now.ToString()
      };

      MessageObject<NewsObject> message = new MessageObject<NewsObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = newsObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateNewsObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetNewsByShow(showId)).Count);
    }

    [Fact]
    public async Task CannotCreateNewsObjectWithoutDoctype()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      NewsObject newsObject = new NewsObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Name = "Example Image",
        Url = "http://somenewswebsite.co.uk/news",
        DatePublished = DateTime.Now.ToString()
      };

      MessageObject<NewsObject> message = new MessageObject<NewsObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = newsObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateNewsObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetNewsByShow(showId)).Count);
    }

    [Fact]
    public async Task CannotCreateNewsObjectWithoutName()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      NewsObject newsObject = new NewsObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Doctype = DocTypes.Image,
        Url = "http://somenewswebsite.co.uk/news",
        DatePublished = DateTime.Now.ToString()
      };

      MessageObject<NewsObject> message = new MessageObject<NewsObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = newsObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateNewsObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetNewsByShow(showId)).Count);
    }

    [Fact]
    public async Task CannotCreateNewsObjectWithoutUrl()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      NewsObject newsObject = new NewsObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Doctype = DocTypes.Image,
        Name = "Example Image",
        DatePublished = DateTime.Now.ToString()
      };

      MessageObject<NewsObject> message = new MessageObject<NewsObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = newsObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateNewsObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetNewsByShow(showId)).Count);
    }

    [Fact]
    public async Task CannotCreateNewsObjectWithoutDatePublished()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      NewsObject newsObject = new NewsObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Doctype = DocTypes.Image,
        Name = "Example Image",
        Url = "http://somenewswebsite.co.uk/news"
      };

      MessageObject<NewsObject> message = new MessageObject<NewsObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = newsObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateNewsObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetNewsByShow(showId)).Count);
    }

    [Fact]
    public async Task CanCreateImagesFromSearch()
    {
      String showId = "5";
      int count = 10;
      ILogger log = new StubLogger();
      ShowObject showObject = new ShowObject()
      {
        Id = showId,
        Partition = showId,
        Doctype = DocTypes.Image,
        ShowName = "Phantom of the Opera"
      };

      MessageObject<ShowObject> message = new MessageObject<ShowObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = showObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateImageObjectsFromSearch(message, log, count));

      // Assert
      Assert.Null(ex);
      Assert.Equal(count, (await _showDomain.GetImageByShow(showId)).Count);
    }


    [Fact]
    public async Task CannotCreateImageWithoutName()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      ImageObject imageObject = new ImageObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Doctype = DocTypes.Image,
        ContentUrl = "http://localhost/something.jpg"
      };


      MessageObject<ImageObject> message = new MessageObject<ImageObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = imageObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateImageObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetImageByShow(showId)).Count);
    }


    [Fact]
    public async Task CannotCreateImageWithoutContentUrl()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      ImageObject imageObject = new ImageObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Doctype = DocTypes.Image,
        Name = "Example Image"
      };

      MessageObject<ImageObject> message = new MessageObject<ImageObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = imageObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateImageObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetImageByShow(showId)).Count);
    }

    [Fact]
    public async Task CannotCreateImageWithoutId()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      ImageObject imageObject = new ImageObject()
      {
        Partition = showId,
        Doctype = DocTypes.Image,
        Name = "Example Image",
        ContentUrl = "http://localhost/something.jpg"
      };

      MessageObject<ImageObject> message = new MessageObject<ImageObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = imageObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateImageObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetImageByShow(showId)).Count);
    }


    [Fact]
    public async Task CannotCreateImageWithoutPartition()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      ImageObject imageObject = new ImageObject()
      {
        Id = Guid.NewGuid().ToString(),
        Doctype = DocTypes.Image,
        Name = "Example Image",
        ContentUrl = "http://localhost/something.jpg"
      };

      MessageObject<ImageObject> message = new MessageObject<ImageObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = imageObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateImageObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetImageByShow(showId)).Count);
    }


    [Fact]
    public async Task CannotCreateImageWithoutDoctype()
    {
      String showId = "5";
      ILogger log = new StubLogger();
      ImageObject imageObject = new ImageObject()
      {
        Id = Guid.NewGuid().ToString(),
        Partition = showId,
        Name = "Example Image",
        ContentUrl = "http://localhost/something.jpg"
      };

      MessageObject<ImageObject> message = new MessageObject<ImageObject>
      {
        Headers = new MessageHeaders()
        {
          RequestCorrelationId = Guid.NewGuid().ToString(),
          RequestCreatedAt = DateTime.Now
        },
        Body = imageObject
      };

      // Act
      var ex = await Record.ExceptionAsync(() => _showDomain.CreateImageObject(message, log));

      // Assert
      Assert.NotNull(ex);
      Assert.Equal(0, (await _showDomain.GetImageByShow(showId)).Count);
    }
  }

}
