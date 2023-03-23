# Notes on PDF Document Part (DPart) Metadata (DPM)

* The document catalog points to the DPartRoot ``/DPartRoot 1880 0 R``

* Here is a sample DPartRootNode:

		1880 0 obj
		<<
			/Type /DPartRoot
			/DPartRootNode 830 0 R
			/RecordLevel 1 % optional, level at which hierarchy branches to leafs(?)
			/NodeNameList [ /Root /Record /Page ] % optional, provide levels of the tree with names
		>>
		endobj

* A DPart intermediate node dictionary; because the ``DPartRoot`` points to it, this is the actual root node of the DPart tree:

		830 0 obj
		<<
			/Type /DPart
			/Parent 1880 0 R
			/DParts [ [ 831 0 R 947 0 R 1063 0 R 1180 0 R 1296 0 R 1412 0 R 1529 0 R 1647 0 R 1763 0 R 1879 0 R ]] % points to the DPart child nodes
		>>
		endobj

* Below is another sample DPart intermediate node dictionary:

		831 0 obj
		<<
			/Type /DPart
			/Parent 830 0 R
			/DPM << % DPart Metadata
				/Record 1
				/RecordValues <<
					/Address (3330 Bay Rd.)
					% ... other fields
				>>
			>>
			/DParts [ [ 803 0 R 805 0 R 807 0 R 809 0 R 811 0 R 813 0 R 815 0 R 817 0 R 819 0 R 821 0 R 825 0 R 827 0 R 829 0 R ]] % points to the DPart child nodes
		>>
		endobj

* Below is a sample DPart leaf node dictionary with start and end pages and inline DPM metadata using PDF dictionary syntax:

		803 0 obj
		<<
			/Type /DPart
			/Parent 831 0 R % points to the DPart parent node
			/Start 801 0 R	% points to the first page in the DPart
			/DPM << % DPart Metadata
				/MediaType (Booklet)
				/Xerox <<
					/Media <<
						/MediaType (Booklet)
					>>
				>>
			>>
		>>
		endobj

* Each page dictionary with a corresponding DPart has a /DPart entry which contains an indirect pointer to a DPart dictionary, such as  ``/DPart 803 0 R``





