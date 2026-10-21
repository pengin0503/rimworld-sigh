using SimpleSigh.Text;
using Xunit;
namespace SimpleSigh.Tests;
public sealed class SignLayoutTests
{
    private static float Measure(string s) => s.Length;
    [Fact] public void EmptyTextFits() {var r=SignLayout.Fit("", 8,2,Measure,1,0.5f);Assert.True(r.Fits);}
    [Fact] public void ManualNewlinesRetained() {var r=SignLayout.Fit("倉庫\n研究",8,3,Measure,1,0.5f);Assert.True(r.Fits);Assert.Equal(2,r.Lines.Count);}
    [Fact] public void JapaneseCanWrapWithoutSpaces() {var r=SignLayout.Fit("あいうえおかきく",4,3,Measure,1,0.5f);Assert.True(r.Fits);Assert.Equal(2,r.Lines.Count);}
    [Fact] public void SingleLineCanShrink() {var r=SignLayout.Fit("ABCDEFGHIJ",5,1,Measure,1,0.4f,false);Assert.True(r.Fits);Assert.True(r.Scale<=0.5f+0.001f);Assert.Single(r.Lines);}
    [Fact] public void ImpossibleAtMinimumScaleReported() {var r=SignLayout.Fit("ABCDEFGHIJ",1,1,Measure,1,0.6f,false);Assert.False(r.Fits);}
}
