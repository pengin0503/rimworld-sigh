using SimpleSigh.Text;
using Xunit;
namespace SimpleSigh.Tests;
public sealed class SignEditSessionTests
{
    [Fact] public void DraftNeverChangesOriginal() { var session=new SignEditSession("old",64,false);session.Draft="new";Assert.Equal("old",session.Original); }
    [Fact] public void ConfirmReturnsNewString() { var session=new SignEditSession("old",64,false){ Draft="倉庫" };Assert.True(session.TryConfirm(out var value,out _)); Assert.Equal("倉庫",value); }
    [Fact] public void CancelRestoresDraft() {var s=new SignEditSession("original",64,false){Draft="changed"};s.Cancel();Assert.Equal("original",s.Draft); }
    [Fact] public void OverflowCannotConfirm() {var s=new SignEditSession("old",64,false){Draft=new string('a',65)};Assert.False(s.TryConfirm(out _,out _));Assert.Equal("old",s.Original); }
}
