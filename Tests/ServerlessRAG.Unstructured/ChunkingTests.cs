using NUnit.Framework.Legacy;
using ServerlessRAG.Unstructured.Models;
using ServerlessRAG.Unstructured;
using ServerlessRAG.Unstructured.Enums;
namespace Tests.ServerlessRAG.Unstructured
{
    [TestFixture]
    public class ChunkerTests
    {
        /// <summary>
        /// Creates a set of sample elements for testing.
        /// </summary>
        private List<Element> GetSampleElements()
        {
            return new List<Element>
            {
                new Element
                {
                    Type = "Title",
                    ElementId = "1",
                    Text = "Introduction",
                    Metadata = new Metadata
                    {
                        PageNumber = 1,
                        Filetype = "application/pdf",
                        Languages = new List<string> { "eng" },
                        Filename = "document.pdf",
                        ParentId = null
                    }
                },
                new Element
                {
                    Type = "NarrativeText",
                    ElementId = "2",
                    Text = "This is the first paragraph.",
                    Metadata = new Metadata
                    {
                        PageNumber = 1,
                        Filetype = "application/pdf",
                        Languages = new List<string> { "eng" },
                        Filename = "document.pdf",
                        ParentId = "1"
                    }
                },
                new Element
                {
                    Type = "NarrativeText",
                    ElementId = "3",
                    Text = "This is the second paragraph.",
                    Metadata = new Metadata
                    {
                        PageNumber = 1,
                        Filetype = "application/pdf",
                        Languages = new List<string> { "eng" },
                        Filename = "document.pdf",
                        ParentId = "1"
                    }
                },
                new Element
                {
                    Type = "Table",
                    ElementId = "4",
                    Text = "Table content.",
                    Metadata = new Metadata
                    {
                        PageNumber = 2,
                        Filetype = "application/pdf",
                        Languages = new List<string> { "eng" },
                        Filename = "document.pdf"
                    }
                },
                new Element
                {
                    Type = "NarrativeText",
                    ElementId = "5",
                    Text = "Conclusion paragraph.",
                    Metadata = new Metadata
                    {
                        PageNumber = 2,
                        Filetype = "application/pdf",
                        Languages = new List<string> { "eng" },
                        Filename = "document.pdf"
                    }
                }
            };
        }

        [Test]
        public void ElementBasedChunking_ReturnsOneChunkPerElement()
        {
            var elements = GetSampleElements();
            var chunks = Chunker.ElementBasedChunking(elements);

            Assert.That(chunks.Count, Is.EqualTo(elements.Count), "Each element should produce its own chunk.");

            foreach (var chunk in chunks)
            {
                Assert.That(chunk.Metadata.SourceElementIds.Count, Is.EqualTo(1),
                    "Each chunk should contain exactly one source element ID.");
                var element = elements.First(e => e.ElementId == chunk.Metadata.SourceElementIds.First());
                Assert.That(chunk.Text, Is.EqualTo(element.Text), "Chunk text should equal the element text.");
                Assert.That(chunk.Metadata.ChunkType, Is.EqualTo(element.Type),
                    "Chunk type should match the element type.");
            }
        }

        [Test]
        public void ParentChildGrouping_CombinesParentWithChildren()
        {
            var elements = GetSampleElements();
            var chunks = Chunker.ParentChildGrouping(elements);

            // Expected:
            // - Element "1" (Title) is top-level and has children ("2" and "3").
            // - Elements "4" (Table) and "5" (NarrativeText) are top-level without children.
            Assert.That(chunks.Count, Is.EqualTo(3), "Should produce a chunk for each top-level element.");

            var parentChunk = chunks.FirstOrDefault(c => c.Metadata.SourceElementIds.Contains("1"));
            Assert.That(parentChunk, Is.Not.Null, "Chunk for element '1' should exist.");

            // Check that parent's text and both children are combined.
            StringAssert.Contains("Introduction", parentChunk.Text);
            StringAssert.Contains("This is the first paragraph.", parentChunk.Text);
            StringAssert.Contains("This is the second paragraph.", parentChunk.Text);
            CollectionAssert.AreEquivalent(new List<string> { "1", "2", "3" }, parentChunk.Metadata.SourceElementIds);
        }

        [Test]
        public void PageLevelChunking_GroupsByPageNumber()
        {
            var elements = GetSampleElements();
            var chunks = Chunker.PageLevelChunking(elements);

            // Expected:
            // Page 1: Elements "1", "2", "3"
            // Page 2: Elements "4", "5"
            Assert.That(chunks.Count, Is.EqualTo(2), "There should be 2 chunks, one per page.");

            var page1Chunk = chunks.FirstOrDefault(c => c.Metadata.PageNumbers.Contains(1));
            var page2Chunk = chunks.FirstOrDefault(c => c.Metadata.PageNumbers.Contains(2));
            Assert.That(page1Chunk, Is.Not.Null, "Chunk for page 1 should exist.");
            Assert.That(page2Chunk, Is.Not.Null, "Chunk for page 2 should exist.");

            CollectionAssert.AreEquivalent(new List<string> { "1", "2", "3" }, page1Chunk.Metadata.SourceElementIds);
            CollectionAssert.AreEquivalent(new List<string> { "4", "5" }, page2Chunk.Metadata.SourceElementIds);
        }

        [Test]
        public void SemanticStructuralGrouping_GroupsContiguousNarrativeTextElements()
        {
            var elements = GetSampleElements();
            var chunks = Chunker.SemanticStructuralGrouping(elements);

            // Expected behavior:
            // - Element "1" (Title) is processed on its own.
            // - Elements "2" and "3" (NarrativeText) are grouped.
            // - Element "4" (Table) is on its own.
            // - Element "5" (NarrativeText) appears after a non-narrative, so it's in its own chunk.
            Assert.That(chunks.Count, Is.EqualTo(4), "Expected 4 chunks based on semantic grouping.");

            var narrativeGroup = chunks.FirstOrDefault(c =>
                c.Metadata.SourceElementIds.Contains("2") && c.Metadata.SourceElementIds.Contains("3"));
            Assert.That(narrativeGroup, Is.Not.Null, "A narrative group chunk containing elements '2' and '3' should exist.");
            StringAssert.Contains("This is the first paragraph.", narrativeGroup.Text);
            StringAssert.Contains("This is the second paragraph.", narrativeGroup.Text);
        }

        [Test]
        public void SlidingWindowChunking_SplitsLongChunkCorrectly()
        {
            // Create a chunk with long text.
            string longText = new string('A', 120); // 120 characters long
            var chunk = new Chunk
            {
                Text = longText,
                Metadata = new ChunkMetadata
                {
                    SourceElementIds = new List<string> { "long" },
                    PageNumbers = new List<int> { 1 },
                    ChunkType = "LongText"
                }
            };

            int maxChunkSize = 50;
            int overlap = 10;
            var slidingChunks = Chunker.SlidingWindowChunking(chunk, maxChunkSize, overlap);

            // Based on our algorithm:
            // - First chunk: characters 0-49 (50 characters)
            // - Second chunk: characters 40-89 (50 characters)
            // - Third chunk: characters 80-119 (40 characters)
            Assert.That(slidingChunks.Count, Is.EqualTo(3),
                "Should produce 3 sliding chunks for a 120-character text with 50 max size and 10 overlap.");
            Assert.That(slidingChunks[0].Text.Length, Is.EqualTo(50), "First chunk should have 50 characters.");
            Assert.That(slidingChunks[1].Text.Length, Is.EqualTo(50), "Second chunk should have 50 characters.");
            Assert.That(slidingChunks[2].Text.Length, Is.EqualTo(40), "Third chunk should have 40 characters.");

            // Verify overlap: the last 10 characters of the first chunk should match the first 10 characters of the second.
            string firstOverlap = slidingChunks[0].Text.Substring(40, 10);
            string secondOverlap = slidingChunks[1].Text.Substring(0, 10);
            Assert.That(firstOverlap, Is.EqualTo(secondOverlap), "Chunks should overlap by 10 characters.");
        }

        [Test]
        public void ContentSpecificChunking_GroupsNarrativeAndTitleSeparately()
        {
            // Create sample elements for content-specific grouping.
            var elements = new List<Element>
            {
                new Element
                {
                    Type = "Title",
                    ElementId = "A",
                    Text = "Header",
                    Metadata = new Metadata
                    {
                        PageNumber = 1,
                        Filetype = "application/pdf",
                        Languages = new List<string> { "eng" },
                        Filename = "doc.pdf"
                    }
                },
                new Element
                {
                    Type = "NarrativeText",
                    ElementId = "B",
                    Text = "Paragraph content",
                    Metadata = new Metadata
                    {
                        PageNumber = 1,
                        Filetype = "application/pdf",
                        Languages = new List<string> { "eng" },
                        Filename = "doc.pdf"
                    }
                },
                new Element
                {
                    Type = "Table",
                    ElementId = "C",
                    Text = "Table data",
                    Metadata = new Metadata
                    {
                        PageNumber = 2,
                        Filetype = "application/pdf",
                        Languages = new List<string> { "eng" },
                        Filename = "doc.pdf"
                    }
                }
            };

            var chunks = Chunker.ContentSpecificChunking(elements);
            // Expected:
            // - One combined chunk for Title ("A") and NarrativeText ("B").
            // - One separate chunk for Table ("C").
            Assert.That(chunks.Count, Is.EqualTo(2), "Should produce 2 chunks for content-specific grouping.");

            var narrativeChunk = chunks.FirstOrDefault(c =>
                c.Metadata.SourceElementIds.Contains("A") && c.Metadata.SourceElementIds.Contains("B"));
            Assert.That(narrativeChunk, Is.Not.Null, "A chunk combining Title and NarrativeText should exist.");
            StringAssert.Contains("Header", narrativeChunk.Text);
            StringAssert.Contains("Paragraph content", narrativeChunk.Text);

            var tableChunk = chunks.FirstOrDefault(c => c.Metadata.SourceElementIds.Contains("C"));
            Assert.That(tableChunk, Is.Not.Null, "The Table element should be in its own chunk.");
            StringAssert.Contains("Table data", tableChunk.Text);
        }

        [Test]
        public void ChunkByTitle_ChunksCorrectly_WithMaxPagesFlush()
        {
            // Arrange: Create sample elements in order.
            var elements = new List<Element>
            {
                // Chunk 1: Title on page 1 with narrative on pages 1 and 2.
                new Element
                {
                    ElementId = "A",
                    Type = "Title",
                    Text = "Title 1",
                    Metadata = new Metadata { PageNumber = 1, Filetype = "application/pdf", Languages = new List<string> {"eng"}, Filename = "doc.pdf" }
                },
                new Element
                {
                    ElementId = "B",
                    Type = "NarrativeText",
                    Text = "Content 1",
                    Metadata = new Metadata { PageNumber = 1, Filetype = "application/pdf", Languages = new List<string> {"eng"}, Filename = "doc.pdf" }
                },
                new Element
                {
                    ElementId = "C",
                    Type = "NarrativeText",
                    Text = "Content 2",
                    Metadata = new Metadata { PageNumber = 2, Filetype = "application/pdf", Languages = new List<string> {"eng"}, Filename = "doc.pdf" }
                },
                // Chunk 2: New Title on page 3 with narrative on page 3.
                new Element
                {
                    ElementId = "D",
                    Type = "Title",
                    Text = "Title 2",
                    Metadata = new Metadata { PageNumber = 3, Filetype = "application/pdf", Languages = new List<string> {"eng"}, Filename = "doc.pdf" }
                },
                new Element
                {
                    ElementId = "E",
                    Type = "NarrativeText",
                    Text = "Content 3",
                    Metadata = new Metadata { PageNumber = 3, Filetype = "application/pdf", Languages = new List<string> {"eng"}, Filename = "doc.pdf" }
                },
                // Chunk 3: No title appears, but narrative element on page 5 triggers flush (maxPagesWithoutTitle = 2).
                new Element
                {
                    ElementId = "F",
                    Type = "NarrativeText",
                    Text = "Content 4",
                    Metadata = new Metadata { PageNumber = 5, Filetype = "application/pdf", Languages = new List<string> {"eng"}, Filename = "doc.pdf" }
                }
            };

            // Act: Use the new ChunkByTitle strategy with maxPagesWithoutTitle = 2.
            var chunks = Chunker.ChunkByTitle(elements, maxPagesWithoutTitle: 2);

            // Assert: We expect 3 chunks.
            // Chunk 1 should contain elements A, B, C.
            // Chunk 2 should contain elements D, E.
            // Chunk 3 should contain element F.
            Assert.That(chunks.Count, Is.EqualTo(3), "There should be 3 chunks when using the title-based strategy with page flush.");

            var chunk1 = chunks[0];
            CollectionAssert.AreEquivalent(new List<string> { "A", "B", "C" }, chunk1.Metadata.SourceElementIds);
            StringAssert.Contains("Title 1", chunk1.Text);
            StringAssert.Contains("Content 1", chunk1.Text);
            StringAssert.Contains("Content 2", chunk1.Text);

            var chunk2 = chunks[1];
            CollectionAssert.AreEquivalent(new List<string> { "D", "E" }, chunk2.Metadata.SourceElementIds);
            StringAssert.Contains("Title 2", chunk2.Text);
            StringAssert.Contains("Content 3", chunk2.Text);

            var chunk3 = chunks[2];
            CollectionAssert.AreEquivalent(new List<string> { "F" }, chunk3.Metadata.SourceElementIds);
            StringAssert.Contains("Content 4", chunk3.Text);
        }

        [Test]
        public void FixedSizeChunking_SplitsTextIntoNonOverlappingSegments()
        {
            // Arrange: Create a chunk with a known text.
            string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // 26 characters
            var originalChunk = new Chunk
            {
                Text = text,
                Metadata = new ChunkMetadata
                {
                    SourceElementIds = new List<string> { "fixed1" },
                    PageNumbers = new List<int> { 1 },
                    ChunkType = "Combined",
                    Strategy = ChunkingStrategy.Combined
                }
            };

            int fixedSize = 10;

            // Act: Use FixedSizeChunking
            List<Chunk> fixedChunks = Chunker.FixedSizeChunking(originalChunk, fixedSize);

            // Assert:
            // Expect 3 chunks: 10, 10, and 6 characters respectively.
            Assert.That(fixedChunks.Count, Is.EqualTo(3), "Expected 3 fixed-size chunks.");
            Assert.That(fixedChunks[0].Text.Length, Is.EqualTo(10), "First chunk should have 10 characters.");
            Assert.That(fixedChunks[1].Text.Length, Is.EqualTo(10), "Second chunk should have 10 characters.");
            Assert.That(fixedChunks[2].Text.Length, Is.EqualTo(6), "Third chunk should have 6 characters.");

            // Verify that each chunk has the correct strategy.
            foreach (var chunk in fixedChunks)
            {
                Assert.That(chunk.Metadata.Strategy, Is.EqualTo(ChunkingStrategy.FixedSize),
                    "Each fixed size chunk should have the FixedSize strategy.");
            }
        }

        [Test]
        public void RecursiveCharacterTextSplitting_SplitsUsingNaturalDelimiters()
        {
            // Arrange: Create a chunk with natural delimiters.
            // Note: This text contains spaces and punctuation that serve as potential breakpoints.
            string text = "Hello, world! This is a test sentence. Enjoy splitting!";
            var originalChunk = new Chunk
            {
                Text = text,
                Metadata = new ChunkMetadata
                {
                    SourceElementIds = new List<string> { "rec1" },
                    PageNumbers = new List<int> { 1 },
                    ChunkType = "Combined",
                    Strategy = ChunkingStrategy.Combined
                }
            };

            int maxChunkSize = 20;

            // Act: Use RecursiveCharacterTextSplitting
            List<Chunk> recursiveChunks = Chunker.RecursiveCharacterTextSplitting(originalChunk, maxChunkSize);

            // Assert:
            // Each resulting chunk should have a length less than or equal to maxChunkSize.
            // Also verify that the Strategy metadata is set to RecursiveCharacter.
            Assert.That(recursiveChunks, Is.Not.Empty, "Should produce at least one recursive chunk.");

            foreach (var chunk in recursiveChunks)
            {
                // Because we trim each chunk, its length might be less than maxChunkSize.
                Assert.That(chunk.Text.Length, Is.LessThanOrEqualTo(maxChunkSize),
                    "Each recursive chunk's length should be less than or equal to the maximum allowed.");
                Assert.That(chunk.Metadata.Strategy, Is.EqualTo(ChunkingStrategy.RecursiveCharacter),
                    "Chunk should have the RecursiveCharacter strategy.");
            }

            // Optionally, you can concatenate the chunks (with a single space between them)
            // and verify that the combined trimmed text is similar to the original (ignoring minor whitespace differences).
            string combinedRecursiveText = string.Join(" ", recursiveChunks.Select(c => c.Text)).Trim();
            Assert.That(combinedRecursiveText.Replace("  ", " "), Is.EqualTo(text.Trim()),
                "The concatenated recursive chunks should equal the original text (ignoring extra whitespace).");
        }

        [Test]
        public void RecursiveCharacterTextSplitting_ForcesBreakWhenNoDelimiterFound()
        {
            // Arrange: Create a chunk with text that has no natural delimiters.
            string text = new string('A', 26); // 26 A's, no spaces or punctuation
            var originalChunk = new Chunk
            {
                Text = text,
                Metadata = new ChunkMetadata
                {
                    SourceElementIds = new List<string> { "rec2" },
                    PageNumbers = new List<int> { 1 },
                    ChunkType = "Combined",
                    Strategy = ChunkingStrategy.Combined
                }
            };

            int maxChunkSize = 10;
            // Act: Use RecursiveCharacterTextSplitting
            List<Chunk> recursiveChunks = Chunker.RecursiveCharacterTextSplitting(originalChunk, maxChunkSize);

            // Assert:
            // Since there are no delimiters, the text should be split forcibly.
            // We expect: 10, 10, and 6 characters.
            Assert.That(recursiveChunks.Count, Is.EqualTo(3), "Should produce 3 forced splits.");
            Assert.That(recursiveChunks[0].Text.Length, Is.EqualTo(10), "First chunk should have 10 characters.");
            Assert.That(recursiveChunks[1].Text.Length, Is.EqualTo(10), "Second chunk should have 10 characters.");
            Assert.That(recursiveChunks[2].Text.Length, Is.EqualTo(6), "Third chunk should have 6 characters.");

            foreach (var chunk in recursiveChunks)
            {
                Assert.That(chunk.Metadata.Strategy, Is.EqualTo(ChunkingStrategy.RecursiveCharacter),
                    "Each recursive chunk should have the RecursiveCharacter strategy.");
            }
        }
    }
}
