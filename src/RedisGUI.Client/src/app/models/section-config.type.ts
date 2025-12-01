export type SectionConfig<TEntity> = ColumnDefinition<TEntity>[];

export type ColumnDefinition<TEntity>  = {
  name: string;
  title?: string;
  hidden?: boolean;
  transformer?: (row: TEntity) => string;
};
