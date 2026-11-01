using PokeTeams.Core.Teams;

namespace PokeTeams.Core.Tests;

public class TeamTests
{
    [Fact]
    public void Add_SeventhMember_IsRejected()
    {
        var team = new Team();
        for (var id = 1; id <= Team.MaxSize; id++)
        {
            Assert.True(team.Add(TestData.Make(id, $"mon{id}")));
        }

        Assert.False(team.Add(TestData.Make(7, "mon7")));
        Assert.Equal(6, team.Members.Count);
        Assert.True(team.IsFull);
    }

    [Fact]
    public void Add_Duplicate_IsRejected()
    {
        var team = new Team();

        Assert.True(team.Add(TestData.Pikachu));
        Assert.False(team.Add(TestData.Pikachu));
        Assert.Single(team.Members);
    }

    [Fact]
    public void Remove_ExistingMember_Works()
    {
        var team = new Team();
        team.Add(TestData.Bulbasaur);
        team.Add(TestData.Pikachu);

        Assert.True(team.Remove(TestData.Bulbasaur.Id));
        Assert.False(team.Contains(TestData.Bulbasaur.Id));
        Assert.Single(team.Members);
    }

    [Fact]
    public void Remove_MissingMember_Fails()
    {
        var team = new Team();

        Assert.False(team.Remove(999));
    }
}
