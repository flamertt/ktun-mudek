import { createColumnHelper } from '@tanstack/react-table'
import { useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'

import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import { DataTable } from '@shared/ui/data-table/DataTable.jsx'
import { formatMudekDecimal } from '../../course-evaluation/lib/mudekDisplayLabels'
import { useCourseEvaluationMudekData } from '../../course-evaluation/model/useCourseEvaluationMudekData'
import { EvaluationMudekBackToolbar } from '../../course-evaluation/ui/EvaluationMudekBackToolbar.jsx'
import styles from './MudekProgramOutcomeResultsPage.module.css'

const columnHelper = createColumnHelper()

export function MudekProgramOutcomeResultsPage() {
  const { offeringId } = useParams()
  const d = useCourseEvaluationMudekData(offeringId)
  const [filter, setFilter] = useState('')

  const rows = useMemo(() => {
    const list = d.mudekResults?.programOutcomeResults ?? d.mudekResults?.ProgramOutcomeResults ?? []
    if (!Array.isArray(list)) return []
    return list.map((x) => {
      const programOutcomeId = x.programOutcomeId ?? x.ProgramOutcomeId
      const raw = String(x.programOutcomeCaption ?? x.ProgramOutcomeCaption ?? '').trim()
      const fromLookup = programOutcomeId ? d.programOutcomeById.get(String(programOutcomeId)) : undefined
      const programOutcomeCaption =
        raw || fromLookup || (programOutcomeId ? `PÇ (${d.shortGuid(programOutcomeId)})` : '—')
      return {
        id: x.id ?? x.Id,
        programOutcomeId,
        programOutcomeCaption,
        achievementScore: x.achievementScore ?? x.AchievementScore,
        updatedAt: x.updatedAt ?? x.UpdatedAt,
      }
    })
  }, [d.mudekResults, d.programOutcomeById, d.shortGuid])

  const sorted = useMemo(() => {
    return [...rows].sort((a, b) => Number(b.achievementScore ?? -Infinity) - Number(a.achievementScore ?? -Infinity))
  }, [rows])

  const ranked = useMemo(() => {
    if (!sorted.length) return []
    const mid = Math.floor(sorted.length / 2)
    return sorted.map((r, i) => ({
      ...r,
      rankClass: i === 0 ? 'rowBest' : i === sorted.length - 1 ? 'rowWorst' : i === mid ? 'rowAvg' : '',
    }))
  }, [sorted])

  const columns = useMemo(
    () => [
      columnHelper.accessor('programOutcomeCaption', {
        header: 'Program çıktısı (PÇ)',
        cell: (info) => info.getValue(),
      }),
      columnHelper.accessor('achievementScore', {
        header: 'Başarı skoru',
        cell: (info) => formatMudekDecimal(info.getValue(), 6),
      }),
      columnHelper.accessor('updatedAt', { header: 'Güncellendi', cell: (info) => d.formatDate(info.getValue()) }),
    ],
    [d],
  )

  return (
    <PageSection
      title={`${d.title} · MÜDEK · Program çıktısı`}
      description="Program çıktısı sonuçları"
      error={d.error}
      loading={d.loading}
    >
      <EvaluationMudekBackToolbar />
      <div className={styles.panel}>
        <h3 className={styles.panelTitle}>MÜDEK · Program çıktısı sonuçları</h3>
        <DataTable
          columns={columns}
          data={ranked}
          globalFilter={filter}
          onGlobalFilterChange={setFilter}
          searchPlaceholder="PÇ kodu, başlık veya skor ara…"
          isLoading={false}
          disablePagination
          getRowClassName={(row) => row?.rankClass ?? ''}
        />
      </div>
    </PageSection>
  )
}

