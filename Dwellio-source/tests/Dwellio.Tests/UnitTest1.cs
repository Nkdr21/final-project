using Dwellio.Core.Models;
using Dwellio.Core.Services;

namespace Dwellio.Tests;

public class CostBoundaryTests
{
    [Fact]
    public void Deposit_does_not_change_monthly_cost()
    {
        var home = new Apartment(1, 1, "Home", 150000, 20000, 0, 1, 40, true, true);
        var complex = new Complex(1, "Complex", "Astana", 0, 0, [], "Demo");
        var preferences = new Preferences(Categories: "");
        var withoutDeposit = RecommendationEngine.CalculateCost(home, complex, preferences);
        var withDeposit = RecommendationEngine.CalculateCost(home with { Deposit = 200000 }, complex, preferences);
        Assert.Equal(withoutDeposit.KnownTotal, withDeposit.KnownTotal);
        Assert.Equal(withoutDeposit.MoveInTotal + 200000, withDeposit.MoveInTotal);
    }
}
