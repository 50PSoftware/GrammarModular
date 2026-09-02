using Grammar.Core.Enums;
using Grammar.Core.Models.Semantics;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies that a symmetric relation, stored once, gives back the correct other side regardless of
    /// which column the caller's own sense happens to sit in.
    /// </summary>
    [TestClass]
    public sealed class SemanticRelationTests
    {
        [TestMethod]
        public void OtherLuId_AnchorIsSideA_ReturnsSideB()
        {
            var relation = new SemanticRelation { LuIdA = 10, LuIdB = 20, RelationType = SemanticRelationType.Synonym };

            Assert.AreEqual(20, relation.OtherLuId(10));
        }

        [TestMethod]
        public void OtherLuId_AnchorIsSideB_ReturnsSideA()
        {
            var relation = new SemanticRelation { LuIdA = 10, LuIdB = 20, RelationType = SemanticRelationType.Synonym };

            Assert.AreEqual(10, relation.OtherLuId(20));
        }
    }
}
