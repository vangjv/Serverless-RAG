using ServerlessRAG.Unstructured.Enums;
using ServerlessRAG.Unstructured.Models;

namespace ServerlessRAG.Unstructured
{
    public static class Chunker
    {
       
        /// <summary>
        /// Strategy 1: Element-based chunking (each element is its own chunk).
        /// </summary>
        public static List<Chunk> ElementBasedChunking(List<Element> elements)
        {
            var chunks = new List<Chunk>();
            foreach (var element in elements)
            {
                var chunk = new Chunk
                {
                    Text = element.Text,
                    Metadata = new ChunkMetadata
                    {
                        SourceElementIds = new List<string> { element.ElementId },
                        PageNumbers = new List<int> { element.Metadata.PageNumber },
                        ChunkType = element.Type,
                        Strategy = ChunkingStrategy.ElementBased
                    }
                };
                chunks.Add(chunk);
            }
            return chunks;
        }

        /// <summary>
        /// Strategy 2: Parent-Child Grouping.
        /// Group top-level elements with their children (if any) using the metadata.parent_id.
        /// </summary>
        public static List<Chunk> ParentChildGrouping(List<Element> elements)
        {
            var chunks = new List<Chunk>();
            // First, build a lookup of children keyed by parent_id.
            var childrenByParent = new Dictionary<string, List<Element>>();
            foreach (var element in elements)
            {
                if (!string.IsNullOrEmpty(element.Metadata.ParentId))
                {
                    if (!childrenByParent.ContainsKey(element.Metadata.ParentId))
                    {
                        childrenByParent[element.Metadata.ParentId] = new List<Element>();
                    }
                    childrenByParent[element.Metadata.ParentId].Add(element);
                }
            }

            // Now, for each top-level element (no parent), create a chunk combining its own text and its children.
            foreach (var element in elements)
            {
                if (string.IsNullOrEmpty(element.Metadata.ParentId))
                {
                    string combinedText = element.Text;
                    List<string> sourceIds = new List<string> { element.ElementId };
                    List<int> pages = new List<int> { element.Metadata.PageNumber };

                    if (childrenByParent.ContainsKey(element.ElementId))
                    {
                        foreach (var child in childrenByParent[element.ElementId])
                        {
                            combinedText += "\n" + child.Text;
                            sourceIds.Add(child.ElementId);
                            pages.Add(child.Metadata.PageNumber);
                        }
                    }

                    chunks.Add(new Chunk
                    {
                        Text = combinedText,
                        Metadata = new ChunkMetadata
                        {
                            SourceElementIds = sourceIds,
                            PageNumbers = pages,
                            ChunkType = "Grouped: " + element.Type,
                            Strategy = ChunkingStrategy.ParentChild
                        }
                    });
                }
            }
            return chunks;
        }

        /// <summary>
        /// Strategy 3: Page-Level Chunking.
        /// Groups elements by their page number.
        /// </summary>
        public static List<Chunk> PageLevelChunking(List<Element> elements)
        {
            var chunks = new List<Chunk>();

            var groups = elements.GroupBy(e => e.Metadata.PageNumber);
            foreach (var group in groups)
            {
                string combinedText = string.Join("\n", group.Select(e => e.Text));
                chunks.Add(new Chunk
                {
                    Text = combinedText,
                    Metadata = new ChunkMetadata
                    {
                        SourceElementIds = group.Select(e => e.ElementId).ToList(),
                        PageNumbers = new List<int> { group.Key },
                        ChunkType = $"Page {group.Key}",
                        Strategy = ChunkingStrategy.PageLevel
                    }
                });
            }
            return chunks;
        }

        /// <summary>
        /// Strategy 4: Semantic or Structural Grouping.
        /// Combines contiguous NarrativeText elements into one chunk.
        /// Other types are output as individual chunks.
        /// </summary>
        public static List<Chunk> SemanticStructuralGrouping(List<Element> elements)
        {
            var chunks = new List<Chunk>();
            var buffer = new List<Element>();

            foreach (var element in elements)
            {
                if (element.Type == "NarrativeText")
                {
                    // Buffer narrative text elements.
                    buffer.Add(element);
                }
                else
                {
                    // When a non-narrative element is encountered,
                    // flush the buffered narrative text as one chunk.
                    if (buffer.Any())
                    {
                        string combinedText = string.Join("\n", buffer.Select(b => b.Text));
                        chunks.Add(new Chunk
                        {
                            Text = combinedText,
                            Metadata = new ChunkMetadata
                            {
                                SourceElementIds = buffer.Select(b => b.ElementId).ToList(),
                                PageNumbers = buffer.Select(b => b.Metadata.PageNumber).Distinct().ToList(),
                                ChunkType = "NarrativeGroup",
                                Strategy = ChunkingStrategy.SemanticStructural
                            }
                        });
                        buffer.Clear();
                    }
                    // Add the non-narrative element as its own chunk.
                    chunks.Add(new Chunk
                    {
                        Text = element.Text,
                        Metadata = new ChunkMetadata
                        {
                            SourceElementIds = new List<string> { element.ElementId },
                            PageNumbers = new List<int> { element.Metadata.PageNumber },
                            ChunkType = element.Type,
                            Strategy = ChunkingStrategy.SemanticStructural
                        }
                    });
                }
            }
            // Flush any remaining narrative text.
            if (buffer.Any())
            {
                string combinedText = string.Join("\n", buffer.Select(b => b.Text));
                chunks.Add(new Chunk
                {
                    Text = combinedText,
                    Metadata = new ChunkMetadata
                    {
                        SourceElementIds = buffer.Select(b => b.ElementId).ToList(),
                        PageNumbers = buffer.Select(b => b.Metadata.PageNumber).Distinct().ToList(),
                        ChunkType = "NarrativeGroup",
                        Strategy = ChunkingStrategy.SemanticStructural
                    }
                });
            }
            return chunks;
        }

        /// <summary>
        /// Strategy 5: Sliding Window / Overlapping Chunking.
        /// For long chunks (exceeding a given max size), break them into overlapping sub-chunks.
        /// </summary>
        public static List<Chunk> SlidingWindowChunking(Chunk chunk, int maxChunkSize, int overlap)
        {
            var chunks = new List<Chunk>();

            if (chunk.Text.Length <= maxChunkSize)
            {
                chunks.Add(chunk);
                return chunks;
            }

            int start = 0;
            while (start < chunk.Text.Length)
            {
                int length = Math.Min(maxChunkSize, chunk.Text.Length - start);
                string part = chunk.Text.Substring(start, length);

                // Create a new chunk preserving the original metadata (with a note on the sliding nature)
                var newChunk = new Chunk
                {
                    Text = part,
                    Metadata = new ChunkMetadata
                    {
                        SourceElementIds = chunk.Metadata.SourceElementIds,
                        PageNumbers = chunk.Metadata.PageNumbers,
                        ChunkType = chunk.Metadata.ChunkType + " (Sliding)",
                        Strategy = ChunkingStrategy.SlidingWindow
                    }
                };
                chunks.Add(newChunk);

                start += (maxChunkSize - overlap);
            }
            return chunks;
        }

        /// <summary>
        /// Strategy 6: Content-Specific Chunking.
        /// For example, group NarrativeText (and even Title) together while keeping others separate.
        /// </summary>
        public static List<Chunk> ContentSpecificChunking(List<Element> elements)
        {
            var chunks = new List<Chunk>();
            var narrativeBuffer = new List<Element>();

            foreach (var element in elements)
            {
                // Here we decide to group narrative content (and titles) together.
                if (element.Type == "NarrativeText" || element.Type == "Title")
                {
                    narrativeBuffer.Add(element);
                }
                else
                {
                    if (narrativeBuffer.Any())
                    {
                        string combinedText = string.Join("\n", narrativeBuffer.Select(e => e.Text));
                        chunks.Add(new Chunk
                        {
                            Text = combinedText,
                            Metadata = new ChunkMetadata
                            {
                                SourceElementIds = narrativeBuffer.Select(e => e.ElementId).ToList(),
                                PageNumbers = narrativeBuffer.Select(e => e.Metadata.PageNumber).Distinct().ToList(),
                                ChunkType = "NarrativeGroup",
                                Strategy = ChunkingStrategy.ContentSpecific
                            }
                        });
                        narrativeBuffer.Clear();
                    }

                    // Process non-narrative element individually.
                    chunks.Add(new Chunk
                    {
                        Text = element.Text,
                        Metadata = new ChunkMetadata
                        {
                            SourceElementIds = new List<string> { element.ElementId },
                            PageNumbers = new List<int> { element.Metadata.PageNumber },
                            ChunkType = element.Type,
                            Strategy = ChunkingStrategy.ContentSpecific
                        }
                    });
                }
            }

            if (narrativeBuffer.Any())
            {
                string combinedText = string.Join("\n", narrativeBuffer.Select(e => e.Text));
                chunks.Add(new Chunk
                {
                    Text = combinedText,
                    Metadata = new ChunkMetadata
                    {
                        SourceElementIds = narrativeBuffer.Select(e => e.ElementId).ToList(),
                        PageNumbers = narrativeBuffer.Select(e => e.Metadata.PageNumber).Distinct().ToList(),
                        ChunkType = "NarrativeGroup",
                        Strategy = ChunkingStrategy.ContentSpecific
                    }
                });
            }

            return chunks;
        }

        /// <summary>
        /// Strategy 7: Chunk By Title.
        /// This strategy starts a new chunk whenever a Title element is encountered.
        /// If no new title appears after a configurable number of pages, the chunk is flushed.
        /// 
        /// If maxPagesWithoutTitle is 0, then no forced page flush is applied.
        /// </summary>
        /// <param name="elements">The list of elements (assumed to be in document order).</param>
        /// <param name="maxPagesWithoutTitle">
        /// The maximum number of pages to span in a chunk if no new title is encountered.
        /// A value of 0 means no forced flush.
        /// </param>
        /// <returns>A list of chunks aggregated by title and pages.</returns>
        public static List<Chunk> ChunkByTitle(List<Element> elements, int maxPagesWithoutTitle = 0)
        {
            var chunks = new List<Chunk>();
            var currentChunk = new List<Element>();
            int? startPage = null;

            foreach (var element in elements)
            {
                if (element.Type == "Title")
                {
                    // When encountering a Title, flush any existing chunk.
                    if (currentChunk.Any())
                    {
                        chunks.Add(CreateChunkFromElements(currentChunk, ChunkingStrategy.TitleBased));
                        currentChunk.Clear();
                    }
                    // Start a new chunk with the title.
                    currentChunk.Add(element);
                    startPage = element.Metadata.PageNumber;
                }
                else
                {
                    if (!currentChunk.Any())
                    {
                        // If no chunk is started yet, start one using the current element.
                        currentChunk.Add(element);
                        startPage = element.Metadata.PageNumber;
                    }
                    else
                    {
                        // Check if we've spanned enough pages without encountering a new title.
                        if (maxPagesWithoutTitle > 0 && element.Metadata.PageNumber - startPage >= maxPagesWithoutTitle)
                        {
                            // Flush the current chunk and start a new one.
                            chunks.Add(CreateChunkFromElements(currentChunk, ChunkingStrategy.TitleBased));
                            currentChunk.Clear();
                            currentChunk.Add(element);
                            startPage = element.Metadata.PageNumber;
                        }
                        else
                        {
                            // Otherwise, continue accumulating.
                            currentChunk.Add(element);
                        }
                    }
                }
            }

            // Flush any remaining elements.
            if (currentChunk.Any())
            {
                chunks.Add(CreateChunkFromElements(currentChunk, ChunkingStrategy.TitleBased));
            }
            return chunks;
        }

        private static Chunk CreateChunkFromElements(List<Element> elements, ChunkingStrategy strategy)
        {
            return new Chunk
            {
                Text = string.Join("\n", elements.Select(e => e.Text)),
                Metadata = new ChunkMetadata
                {
                    SourceElementIds = elements.Select(e => e.ElementId).ToList(),
                    PageNumbers = elements.Select(e => e.Metadata.PageNumber).Distinct().ToList(),
                    ChunkType = strategy.ToString(),
                    Strategy = strategy
                }
            };
        }

        /// <summary>
        /// Combines all Elements into a single chunk.
        /// This combined chunk can then be used with sliding window, fixed size, or recursive splitting.
        /// </summary>
        public static Chunk CombineAllElementsIntoSingleChunk(List<Element> elements)
        {
            string combinedText = string.Join("\n", elements.Select(e => e.Text));
            var sourceIds = elements.Select(e => e.ElementId).ToList();
            var pages = elements.Select(e => e.Metadata.PageNumber).Distinct().ToList();
            return new Chunk
            {
                Text = combinedText,
                Metadata = new ChunkMetadata
                {
                    SourceElementIds = sourceIds,
                    PageNumbers = pages,
                    ChunkType = "Combined",
                    Strategy = ChunkingStrategy.Combined
                }
            };
        }


        /// <summary>
        /// Strategy: Fixed Size Chunking.
        /// Splits a chunk’s text into consecutive, non-overlapping segments of a fixed size.
        /// </summary>
        public static List<Chunk> FixedSizeChunking(Chunk chunk, int fixedSize)
        {
            var chunks = new List<Chunk>();
            if (string.IsNullOrEmpty(chunk.Text))
            {
                return chunks;
            }
            int textLength = chunk.Text.Length;
            int start = 0;
            while (start < textLength)
            {
                int length = Math.Min(fixedSize, textLength - start);
                string subText = chunk.Text.Substring(start, length);
                chunks.Add(new Chunk
                {
                    Text = subText,
                    Metadata = new ChunkMetadata
                    {
                        SourceElementIds = chunk.Metadata.SourceElementIds,
                        PageNumbers = chunk.Metadata.PageNumbers,
                        ChunkType = chunk.Metadata.ChunkType + " (FixedSize)",
                        Strategy = ChunkingStrategy.FixedSize
                    }
                });
                start += fixedSize;
            }
            return chunks;
        }

        /// <summary>
        /// Strategy: Recursive Character Text Splitting.
        /// Recursively splits a chunk’s text into smaller chunks by looking for natural delimiters.
        /// If no delimiter is found within maxChunkSize, it falls back to a forced break.
        /// </summary>
        /// <param name="chunk">The chunk to split.</param>
        /// <param name="maxChunkSize">Maximum allowed size for each sub-chunk.</param>
        /// <param name="delimiters">
        /// Optional array of delimiters to consider as natural breakpoints.
        /// Defaults to space, newline, carriage return, period, comma, and semicolon.
        /// </param>
        public static List<Chunk> RecursiveCharacterTextSplitting(Chunk chunk, int maxChunkSize, char[] delimiters = null)
        {
            if (delimiters == null)
            {
                delimiters = new char[] { ' ', '\n', '\r', '.', ',', ';' };
            }
            var chunks = new List<Chunk>();
            RecursiveSplit(chunk.Text, 0, maxChunkSize, delimiters, chunk, chunks);
            return chunks;
        }

        private static void RecursiveSplit(string text, int offset, int maxChunkSize, char[] delimiters, Chunk originalChunk, List<Chunk> result)
        {
            if (offset >= text.Length)
                return;

            int remaining = text.Length - offset;
            if (remaining <= maxChunkSize)
            {
                // No further splitting needed.
                result.Add(new Chunk
                {
                    Text = text.Substring(offset), // preserve exact text
                    Metadata = new ChunkMetadata
                    {
                        SourceElementIds = originalChunk.Metadata.SourceElementIds,
                        PageNumbers = originalChunk.Metadata.PageNumbers,
                        ChunkType = originalChunk.Metadata.ChunkType + " (Recursive)",
                        Strategy = ChunkingStrategy.RecursiveCharacter
                    }
                });
                return;
            }

            // Consider the substring of length maxChunkSize.
            string subText = text.Substring(offset, maxChunkSize);
            int breakIndex = subText.LastIndexOfAny(delimiters);
            if (breakIndex == -1)
            {
                // No natural delimiter found; force break at maxChunkSize.
                breakIndex = maxChunkSize;
            }
            else
            {
                // Include the delimiter in the current chunk.
                breakIndex = breakIndex + 1;
            }

            result.Add(new Chunk
            {
                Text = text.Substring(offset, breakIndex),
                Metadata = new ChunkMetadata
                {
                    SourceElementIds = originalChunk.Metadata.SourceElementIds,
                    PageNumbers = originalChunk.Metadata.PageNumbers,
                    ChunkType = originalChunk.Metadata.ChunkType + " (Recursive)",
                    Strategy = ChunkingStrategy.RecursiveCharacter
                }
            });

            RecursiveSplit(text, offset + breakIndex, maxChunkSize, delimiters, originalChunk, result);
        }




    }
}
