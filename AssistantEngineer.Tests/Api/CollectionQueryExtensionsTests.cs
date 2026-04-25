using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions;

namespace AssistantEngineer.Tests;

public class CollectionQueryExtensionsTests
{
    [Fact]
    public void ToPagedResponseCapsPageSizeAndCalculatesTotalPages()
    {
        var query = new CollectionQueryParameters
        {
            Page = 1,
            PageSize = 500
        };

        var response = Enumerable.Range(1, 205).ToPagedResponse(query);

        Assert.Equal(CollectionQueryParameters.MaxPageSize, response.PageSize);
        Assert.Equal(205, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
        Assert.Equal(100, response.Items.Count);
        Assert.Equal(1, response.Items[0]);
        Assert.Equal(100, response.Items[^1]);
    }
}
