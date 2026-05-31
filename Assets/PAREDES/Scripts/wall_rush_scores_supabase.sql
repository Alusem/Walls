-- Executar no Supabase: SQL Editor → New query → Run
-- Depois: Authentication pode ficar desativado para só REST com anon key + RLS abaixo.

create table if not exists public.wall_rush_scores (
  id bigint generated always as identity primary key,
  score int not null check (score > 0 and score < 2000000000),
  display_name text not null default 'Jogador',
  created_at timestamptz not null default now()
);

create index if not exists wall_rush_scores_score_desc_idx
  on public.wall_rush_scores (score desc);

alter table public.wall_rush_scores enable row level security;

drop policy if exists "wall_rush_read_top" on public.wall_rush_scores;
create policy "wall_rush_read_top"
  on public.wall_rush_scores for select
  using (true);

drop policy if exists "wall_rush_insert_score" on public.wall_rush_scores;
create policy "wall_rush_insert_score"
  on public.wall_rush_scores for insert
  with check (true);
