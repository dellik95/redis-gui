/**
 * Configuration for a table section
 */
export type SectionConfig<TEntity> = ColumnDefinition<TEntity>[];

/**
 * Definition for a table column
 */
export interface ColumnDefinition<TEntity> {
	/**
	 * Column name (used as property key)
	 */
	name: string;

	/**
	 * Display title for the column header
	 */
	title?: string;

	/**
	 * Whether the column is hidden
	 */
	hidden?: boolean;

	/**
	 * Optional transformer function to format cell value
	 */
	transformer?: (row: TEntity) => string;
}
