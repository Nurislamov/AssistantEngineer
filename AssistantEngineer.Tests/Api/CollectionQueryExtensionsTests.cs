using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;

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

        Assert.Equal(query.GetPageSize(), response.PageSize);
        Assert.Equal(205, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
        Assert.Equal(205, response.Items.Count);
        Assert.Equal(1, response.Items[0]);
        Assert.Equal(205, response.Items[^1]);
    }
}
